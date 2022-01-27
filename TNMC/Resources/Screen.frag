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

vec3 DDAtest(in vec3 ro, in vec3 rd)
{
	vec3 col = vec3(0.0);

	vec3 tonext = 1.0 / abs(rd);

	ivec3 cell = ivec3(ro);
	ivec3 cellstep = ivec3(sign(rd));

	//==============================================================================//

	vec3 dists = vec3(1.0);

	dists = fract(ro);

	if(rd.x > 0) dists.x = 1.0 - dists.x;
	if(rd.y > 0) dists.y = 1.0 - dists.y;
	if(rd.z > 0) dists.z = 1.0 - dists.z;

	dists = dists / abs(rd);

	float mindist = min(dists.x, min(dists.y, dists.z));

	vec3 p = ro + rd * mindist;

	//if(dists.x == mindist) cell.x += cellstep.x;
	//if(dists.y == mindist) cell.y += cellstep.y;
	//if(dists.z == mindist) cell.z += cellstep.z;

	//===============================================================================//

	bool hit = false;
	vec3 totdists = tonext + dists;
	float dist = mindist;
	for(int i = 0; i < 256 && !hit; i++)
	{
		//Check to see if we have hit a solid cell
		hit = cell.z <= 0;
		if(hit) continue;
		
		//Do the do with the doobilydo
		float mindist = min(totdists.x, min(totdists.y, totdists.z));
		dist = mindist;

		if(totdists.x == mindist){ totdists.x += tonext.x; cell.x += cellstep.x; }
		if(totdists.y == mindist){ totdists.y += tonext.y; cell.y += cellstep.y; }
		if(totdists.z == mindist){ totdists.z += tonext.z; cell.z += cellstep.z; }
	}
	

	p = ro + rd * dist;
	//p = float(hit) * p;

	col = (fract(p) - 0.5) * 2.0;

	return col;
}

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
	

	fragcol = vec4(DDAtest(cam.pos, rd), 1.0);
}