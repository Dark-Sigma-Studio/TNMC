#version 430
//-------------- Defines --------------//
#define iCoord gl_FragCoord.xy
//-------------------------------------//

out vec4 fragcol;

uniform vec2 iResolution;
const float Pi = atan(0.0, -1.0);

uniform struct camdata
{
	vec3 pos;
	mat3 dirs;
}cam;

vec3 Plane(in vec3 ro, in vec3 rd)
{
	vec3 col = vec3(0.0);

	vec2 uv = fract((ro.z / -rd.z) * rd.xy + ro.xy);

	col = vec3(uv.x, uv.y, 0.0);

	return col;
}

void main()
{
	vec2 uv = 2.0 * (iCoord - iResolution / 2.0) / iResolution.x;
	vec3 rd = vec3(uv, 1.0).xzy * cam.dirs;
	

	fragcol = vec4(Plane(cam.pos, rd), 1.0);
}