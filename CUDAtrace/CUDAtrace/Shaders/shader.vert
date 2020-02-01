#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;

out vec3 v_normal;
out vec3 v_fragpos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform mat4 normal_mat;

void main()
{
    //gl_Position = (projection * view) * ((model * vec4(position, 1.0)) * vec4(-1.0, 1.0, 1.0, 1.0));
    //v_normal = normalize(mat3(normal_mat) * normal) * vec3(-1.0, 1.0, -1.0);
    gl_Position = (projection * view * model) * vec4(position, 1.0);
    v_normal = normalize(mat3(normal_mat) * normal * vec3(1.0, 1.0, -1.0));
    v_fragpos = (model * vec4(position, 1.0)).xyz * vec3(-1.0, 1.0, 1.0);
}