#version 330 core

out vec4 FragColor;

in vec2 uv;

uniform sampler2D skyTexture;

void main()
{
    vec2 fixedUV;

    fixedUV.x = uv.x;
    fixedUV.y = 1.0 - uv.y;

    FragColor = texture(
        skyTexture,
        fixedUV
    );
}