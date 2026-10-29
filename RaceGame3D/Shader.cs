using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace RaceGame3D
{
    public class Shader
    {
        public int Handle;


        public Shader(string vertexPath, string fragmentPath)
        {
            string vertexCode = File.ReadAllText(vertexPath);
            string fragmentCode = File.ReadAllText(fragmentPath);


            int vertex = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertex, vertexCode);
            GL.CompileShader(vertex);
            Console.WriteLine(GL.GetShaderInfoLog(vertex));


            int fragment = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragment, fragmentCode);
            GL.CompileShader(fragment);
            Console.WriteLine(GL.GetShaderInfoLog(fragment));
    

            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, vertex);
            GL.AttachShader(Handle, fragment);

            GL.LinkProgram(Handle);
            Console.WriteLine(GL.GetProgramInfoLog(Handle));

            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);
        }


        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public void SetInt(string name, int value)
        {
            int location = GL.GetUniformLocation(
                Handle,
                name
            );

            GL.Uniform1(
                location,
                value
            );
        }


        public void SetMatrix4(string name, Matrix4 value)
        {
            int location = GL.GetUniformLocation(
                Handle,
                name
            );

            GL.UniformMatrix4(
                location,
                false,
                ref value
            );
        }
    }
}