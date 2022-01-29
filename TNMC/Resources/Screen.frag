#version 430
//-------------- Defines --------------//
#define iCoord gl_FragCoord.xy
//-------------------------------------//

layout(std430, binding = 3) buffer chunk
{
	uint[16][16][16]data;
};

out vec4 fragcol;

uniform vec2 iResolution;
const float Pi = atan(0.0, -1.0);

const int renderdist = 12;

uniform struct camdata
{
	vec3 pos;
	mat3 dirs;
}cam;

bool Check(in ivec3 cell)
{
	bool solid = false;

	//solid = cell.z < 0;

	if(cell.x  < 0 || cell.x >= 16 
	|| cell.y  < 0 || cell.y >= 16 
	|| cell.z  < 0 || cell.z >= 16) return false;

	solid = data[cell.x][cell.y][cell.z] > 0;

	return solid;
}

const vec3[10] gens = 
{
	vec3(0.1, 0.9, 0.7),
	vec3(0.4, 0.6, 0.2),
	vec3(0.7, 0.2, 0.6),
	vec3(0.2, 0.5, 0.0),
	vec3(0.3, 0.7, 0.4),
	vec3(0.6, 0.1, 0.5),
	vec3(0.9, 0.0, 0.4),
	vec3(0.8, 0.4, 0.9),
	vec3(0.0, 0.3, 0.1),
	vec3(0.5, 0.8, 0.3)
};

vec3 CobbleTex(in vec3 uvt)
{
	vec3 texcol = vec3(0.0);
	//============================================================================//
	float dist = 10.0;

	for(int i = 0; i < 8; i++)
	{
		for(int x = -1; x <= 1; x++)
		{
			for(int y = -1; y <= 1; y++)
			{
				for(int z = -1; z <= 1; z++)
				{
					dist = min(dist, distance(uvt, gens[i] + vec3(x, y, z)));
				}
			}
		}
		
	}

	float val = dist + 0.5;
	val = pow(val, 10);

	texcol = vec3(1.0 - val) * 0.5;
	//============================================================================//
	return texcol;
}

void DDAtest(in vec3 ro, in vec3 rd, out vec3 col)
{
	vec3 tonext = 1.0 / abs(rd);

	ivec3 cell = ivec3(floor(ro));
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

	//===============================================================================//

	bool hit = false;
	vec3 totdists = dists;
	float dist = mindist;
	for(int i = 0; i < 16 * renderdist && !hit; i++)
	{
		
		//Do the do with the doobilydo
		float mindist = min(totdists.x, min(totdists.y, totdists.z));
		dist = mindist;

		cell += cellstep * ivec3(
		int(totdists.x == mindist), 
		int(totdists.y == mindist),
		int(totdists.z == mindist));

		//Check the cell...
		hit = Check(cell);
		if(hit) continue;

		totdists += tonext * vec3(
		float(totdists.x == mindist),
		float(totdists.y == mindist),
		float(totdists.z == mindist));
	}
	

	p = ro + rd * dist;

	vec3 uvt = clamp(p - cell, 0.0, 1.0);

	vec3 texcol = CobbleTex(uvt);

	if(hit) col = texcol;
}

void Plane(in vec3 ro, in vec3 rd, out vec3 col)
{
	vec2 uv = fract((ro.z / -rd.z) * rd.xy + ro.xy);

	if(rd.z < 0.0) col = vec3(uv, 0.0);
}

vec3 Sky(in vec3 rd)
{
	vec3 col = vec3(0.1, 0.5, 0.7);

	vec3 dir = normalize(rd);

	col.x = pow(col.x, dir.z * 5.0);
	col.y = pow(col.y, dir.z * 5.0);
	col.z = pow(col.z, dir.z * 5.0);

	return clamp(col, 0.0, 1.0);
}

void main()
{
	vec2 uv = 2.0 * (iCoord - iResolution / 2.0) / iResolution.x;
	vec3 rd = vec3(uv, 1.0).xzy * cam.dirs;
	
	vec3 col = Sky(rd);
	DDAtest(cam.pos, rd, col);

	fragcol = vec4(col, 1.0);
}