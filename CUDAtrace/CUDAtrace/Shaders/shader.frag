#version 330 core
in vec3 v_normal;
in vec3 v_fragpos;

out vec4 fragColor;

uniform vec4 diffuse;
uniform vec4 emission;
uniform vec4 lightPosition;
uniform vec3 lightDirection;
uniform vec4 lightColor;

#define PI 3.14159265

vec3 saturate(vec3 v) 
{
    return clamp(v, vec3(0.0), vec3(1.0));
}

vec3 acesTonemap(vec3 x, float exposure) 
{
    x *= exposure;
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;

    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

void main()
{
    vec3 diff = v_fragpos - lightPosition.xyz;
    float dist = length(diff);
    diff = diff / dist;
    
    float ndotl = max(0, dot(-v_normal, diff));

    float val = 1.0 / ((dist * dist) / (max(dot(diff, lightDirection), 0) * lightPosition.w * lightPosition.w * PI)) / PI;

    vec3 color = ndotl * val * diffuse.xyz * diffuse.w * lightColor.xyz * lightColor.w;

    color += emission.xyz * emission.w;

    fragColor = vec4(acesTonemap(color, 2.0), 1.0);
    //fragColor = vec4(v_normal, 1.0);
    //fragColor = vec4(v_fragpos, 1.0);
    //fragColor = vec4(diffuse.xyz * ndotl, 1.0);
}