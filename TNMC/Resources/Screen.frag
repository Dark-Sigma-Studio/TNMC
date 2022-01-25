#version 430
//-------------- Defines --------------//
#define iCoord gl_FragCoord.xy
//-------------------------------------//

out vec4 fragcol;

void main()
{
	fragcol = vec4(1.0, 0.0, 0.0, 1.0);
}