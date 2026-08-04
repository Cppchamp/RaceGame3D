#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 texCoord;

out vec2 uv;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    mat4 rotView = mat4(mat3(view));

    gl_Position =
        projection *
        rotView *
        vec4(position * 500.0, 1.0);

    uv = texCoord;
}