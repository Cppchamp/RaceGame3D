#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

out vec2 TexCoord;

uniform mat4 _model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    TexCoord = aTexCoord;

    gl_Position =
        projection *
        view *
        _model *
        vec4(aPosition,1.0);
}