/*
 * Copyright LWJGL. All rights reserved.
 * License terms: https://www.lwjgl.org/license
 */
#if defined(GL_core_profile) && !defined(OLD_VERSION)
  #define varying in
  #define texture2D texture

  out vec4 color;
  #define OUT color
#else
  #define OUT gl_FragColor
#endif

uniform sampler2D framebuffer;
uniform sampler2D boxes;
uniform int numBoxes;

uniform vec3 eye;
uniform vec3 ray00;
uniform vec3 ray01;
uniform vec3 ray10;
uniform vec3 ray11;

uniform float blendFactor;
uniform float time;
uniform float width;
uniform float height;
uniform int bounceCount;

varying vec2 texcoord;

struct box {
  vec3 min;
  vec3 max;
} b;

#define MAX_SCENE_BOUNDS 100.0
#define EPSILON 0.00001
#define LIGHT_RADIUS 0.4

#define LIGHT_BASE_INTENSITY 20.0
const vec3 lightCenterPosition = vec3(1.5, 2.9, 3);
const vec4 lightColor = vec4(1);

float random(vec2 f, float time);
vec3 randomDiskPoint(vec3 rand, vec3 n);
vec3 randomHemispherePoint(vec3 rand, vec3 n);
vec3 randomHemisphereCosineWeightedPoint(vec3 rand, vec3 dir);

struct hitinfo {
  vec3 normal;
  float near;
  float far;
  int bi;
};

/*
 * We need random values every now and then.
 * So, they will be precomputed for each ray we trace and
 * can be used by any function.
 */
vec3 rand;
vec3 cameraUp;

box sampleBox(int boxIndex) {
  vec2 minTexel = vec2((2.0 * float(boxIndex) + 0.5) / (2.0*float(numBoxes)), 0.5);
  vec2 maxTexel = vec2((2.0 * float(boxIndex) + 1.0 + 0.5) / (2.0*float(numBoxes)), 0.5);
  vec4 minVal = texture2D(boxes, minTexel);
  vec4 maxVal = texture2D(boxes, maxTexel);
  vec3 min = minVal.rgb;
  vec3 max = maxVal.rgb;
  b.min = min;
  b.max = max;
  return b;
}

vec2 intersectBox(vec3 origin, vec3 dir, const box b, out vec3 normal) {
  vec3 tMin = (b.min - origin) / dir;
  vec3 tMax = (b.max - origin) / dir;
  vec3 t1 = min(tMin, tMax);
  vec3 t2 = max(tMin, tMax);
  float tNear = max(max(t1.x, t1.y), t1.z);
  float tFar = min(min(t2.x, t2.y), t2.z);
  normal = vec3(equal(t1, vec3(tNear))) * sign(-dir);
  return vec2(tNear, tFar);
}

vec4 colorOfBox(const box b) {
  vec4 col;
  col = vec4(0.5, 0.5, 0.5, 1.0);
  return col;
}

bool intersectBoxes(vec3 origin, vec3 dir, out hitinfo info) {
  float smallest = MAX_SCENE_BOUNDS;
  bool found = false;
  vec3 normal;
  for (int i = 0; i < numBoxes; i++) {
    box b = sampleBox(i);
    vec2 lambda = intersectBox(origin, dir, b, normal);
    if (lambda.y >= 0.0 && lambda.x < lambda.y && lambda.x < smallest) {
      info.normal = normal;
      info.near = lambda.x;
      info.far = lambda.y;
      info.bi = i;
      smallest = lambda.x;
      found = true;
    }
  }
  return found;
}

vec4 trace(vec3 origin, vec3 dir) {
  hitinfo i;
  vec4 accumulated = vec4(0.0);
  vec4 attenuation = vec4(1.0);
  for (int bounce = 0; bounce < bounceCount; bounce++) {
    if (intersectBoxes(origin, dir, i)) {
      box b = sampleBox(i.bi);
      vec3 hitPoint = origin + i.near * dir;
      vec3 normal = i.normal;
      vec3 lightNormal = normalize(hitPoint - lightCenterPosition);
      vec3 lightPosition = lightCenterPosition + randomDiskPoint(rand, lightNormal) * LIGHT_RADIUS;
      vec3 shadowRayDir = lightPosition - hitPoint;
      vec3 shadowRayStart = hitPoint + normal * EPSILON;
      hitinfo shadowRayInfo;
      bool lightObstructed = intersectBoxes(shadowRayStart, shadowRayDir, shadowRayInfo);
      attenuation *= colorOfBox(b);
      if (shadowRayInfo.near >= 1.0) {
        float cosineFallOff = max(0.0, dot(normal, normalize(shadowRayDir)));
        float oneOverR2 = 1.0 / dot(shadowRayDir, shadowRayDir);
        accumulated += attenuation * vec4(lightColor * LIGHT_BASE_INTENSITY * cosineFallOff * oneOverR2);
      }
      origin = shadowRayStart;
      dir = randomHemisphereCosineWeightedPoint(rand, normal);
      attenuation *= dot(normal, dir);
    } else {
      break;
    }
  }
  return accumulated;
}

void main(void) {
  vec2 pos = texcoord;
  vec4 newColor = vec4(0.0, 0.0, 0.0, 1.0);
  cameraUp = normalize(ray01 - ray00);
  float rand1 = random(pos, time);
  float rand2 = random(pos + vec2(gl_FragCoord.xy), time);
  float rand3 = random(pos - vec2(gl_FragCoord.xy), time);
  rand = vec3(rand1, rand2, rand3);
  vec2 p = pos;
  vec3 dir = mix(mix(ray00, ray01, p.y), mix(ray10, ray11, p.y), p.x);
  newColor += trace(eye, dir);
  vec4 oldColor = vec4(0.0);
  oldColor = texture2D(framebuffer, pos);
  OUT = mix(newColor, oldColor, blendFactor);
}
