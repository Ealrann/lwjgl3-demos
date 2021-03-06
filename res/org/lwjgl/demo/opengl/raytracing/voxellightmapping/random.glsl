/*
 * Copyright LWJGL. All rights reserved.
 * License terms: https://www.lwjgl.org/license
 */
#version 330 core

#define PI 3.14159265359
#define TWO_PI 6.28318530718
#define ONE_OVER_PI (1.0 / PI)
#define ONE_OVER_2PI (1.0 / TWO_PI)
vec3 ortho(vec3 v) {
  return normalize(abs(v.x) < abs(v.z) ? vec3(0.0, -v.z, v.y) : vec3(-v.y, v.x, 0.0));
}
vec3 around(vec3 v, vec3 z) {
  vec3 t = ortho(z), b = cross(z, t);
  return t * v.x + b * v.y + z * v.z;
}
vec3 isotropic(float rp, float c) {
  float p = TWO_PI * rp, s = sqrt(1.0 - c*c);
  return vec3(cos(p) * s, sin(p) * s, c);
}
vec4 randomCosineWeightedHemisphereDirection(vec3 n, vec2 rand) {
  float c = sqrt(rand.y);
  return vec4(around(isotropic(rand.x, c), n), c * ONE_OVER_PI);
}
uint hash2(uint x, uint y) {
  x += x >> 11;
  x ^= x << 7;
  x += y;
  x ^= x << 6;
  x += x >> 15;
  x ^= x << 5;
  x += x >> 12;
  x ^= x << 9;
  return x;
}
float random2(vec2 f) {
  uint mantissaMask = 0x007FFFFFu, one = 0x3F800000u;
  uvec2 u = floatBitsToUint(f);
  uint h = hash2(u.x, u.y);
  return uintBitsToFloat((h & mantissaMask) | one) - 1.0;
}
vec2 randvec2(float time, float frame) {
  return vec2(random2(vec2(frame*312.3, time * 3.2512)),
              random2(vec2(frame, time + 0.23)));
}
