#version 430
//-------------- Defines --------------//
#define iCoord gl_FragCoord.xy
//-------------------------------------//

layout(std430, binding = 0) buffer data000
{
	uint[128][128][128]chunk000;
};
layout(std430, binding = 1) buffer data001
{
	uint[128][128][128]chunk001;
};
layout(std430, binding = 2) buffer data010
{
	uint[128][128][128]chunk010;
};
layout(std430, binding = 3) buffer data011
{
	uint[128][128][128]chunk011;
};
layout(std430, binding = 4) buffer data100
{
	uint[128][128][128]chunk100;
};
layout(std430, binding = 5) buffer data101
{
	uint[128][128][128]chunk101;
};
layout(std430, binding = 6) buffer data110
{
	uint[128][128][128]chunk110;
};
layout(std430, binding = 7) buffer data111
{
	uint[128][128][128]chunk111;
};

uniform ivec3 selectedcell;
uniform bool hasselection;
vec3 selectlight = vec3(0.0);

out vec4 fragcol;

uniform vec2 iResolution;
const float Pi = atan(0.0, -1.0);

const vec3 sun = normalize(vec3(1.0, 0.5, -0.7));

const int renderdist = 8;

uniform struct camdata
{
	vec3 pos;
	mat3 dirs;
}cam;

bool Check(in ivec3 cell)
{
	bool solid = false;

	if(cell.x  < 0 || cell.x >= 128 
	|| cell.y  < 0 || cell.y >= 128 
	|| cell.z  < 0 || cell.z >= 128) return false;

	solid = chunk000[cell.x][cell.y][cell.z] > 0;

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

vec3 GetFaceNormal(in vec3 UVT)
{
	vec3 uvt = UVT - 0.5;
	vec3 fnorm = uvt;
	float ext = max(abs(fnorm.x), max(abs(fnorm.y), abs(fnorm.z)));
	fnorm *= vec3(abs(fnorm.x) == ext, abs(fnorm.y) == ext, abs(fnorm.z) == ext);
	return normalize(fnorm);
}

vec3 CobbleTex(in vec3 uvt)
{
	vec3 texcol = vec3(0.0);
	//============================================================================//
	float dist = 3.0;

	for(int i = 0; i < 8; i++)
	{
		dist = min(dist, length(fract(gens[i] - uvt) - 0.5));
	}
	
	float thresh = 0.75;
	dist /= thresh;
	dist += 0.4;
	dist = pow(dist, 8);

	float t = clamp(1.0 - dist, 0.0, 1.0);

	texcol = mix(vec3(0.05, 0.05, 0.075), vec3(0.647, 0.635, 0.764), t);
	//============================================================================//
	return texcol;
}

struct HitStruct
{
	bool hit;
	vec3 pos;
	ivec3 cell;
};

void DDAV2 (in vec3 ro, in vec3 rd, inout float disttally, out HitStruct info)
{
	vec3 tonext = 1.0 /  abs(rd);
	ivec3 cellstep = ivec3(sign(rd));
	//==============================================================================//
	// <--------- Set-up for first point of intersection --------->
	vec3 dists = fract(ro);

	if(rd.x > 0) dists.x = 1.0 - dists.x;
	if(rd.y > 0) dists.y = 1.0 - dists.y;
	if(rd.z > 0) dists.z = 1.0 - dists.z;

	dists /= abs(rd);

	float mindist = min(dists.x, min(dists.y, dists.z));

	info.pos = ro + rd * mindist;
	//==============================================================================//
	// <--------- DDA core algorithm --------->
	bool hit = false;
	vec3 totdists = dists;
	float dist = mindist;
	while(!hit && disttally + dist < 16 * renderdist)
	{
		// [DDA stuffs]
		float mindist = min(totdists.x, min(totdists.y, totdists.z));
		dist = mindist;

		info.cell += cellstep * ivec3(
		int(totdists.x == mindist), 
		int(totdists.y == mindist),
		int(totdists.z == mindist));

		// [Check Cell]
		hit = Check(info.cell);
		selectlight = vec3(float(info.cell == selectedcell));
		if(hit) continue;

		// [Step forward allong ray]
		totdists += tonext * vec3(
		float(totdists.x == mindist),
		float(totdists.y == mindist),
		float(totdists.z == mindist));
	}
	//==============================================================================//
	// <--------- Clean-up information --------->
	info.pos = ro + rd * dist;
	disttally += dist;
	info.hit = hit;
	//==============================================================================//
}

void Plane(in vec3 ro, in vec3 rd, out vec3 col)
{
	vec2 uv = fract((ro.z / -rd.z) * rd.xy + ro.xy);

	if(rd.z < 0.0) col = vec3(uv, 0.0);
}

vec3 Sun(in vec3 rd)
{
	vec3 proj = rd - (dot(rd, sun) * sun);
	float size = 0.2;
	return vec3(size / (size + length(proj)));
}

vec3 Sky(in vec3 rd)
{
	vec3 col = vec3(0.1, 0.5, 0.7);

	vec3 dir = normalize(rd);

	float density = 0.2;

	col.x = pow(col.x, dir.z / density);
	col.y = pow(col.y, dir.z / density);
	col.z = pow(col.z, dir.z / density);

	col += Sun(rd);

	return clamp(col, 0.0, 1.0);
}

void GetAlbedo(in vec3 uvt, in bool hit, out vec3 col)
{
	if(hit) col = CobbleTex(uvt);
}

void GetSunLight(inout HitStruct info, inout vec3 light, inout float tdist, in vec3 normal)
{
	if(!info.hit) return;

	vec3 ro = info.pos;
	vec3 rd = -sun;

	DDAV2(ro, rd, tdist, info);

	light = vec3(float(!info.hit) * clamp(dot(normal, -sun), 0.0, 1.0));
}

void GetGlobalIllumination(inout HitStruct info, inout vec3 light)
{
	if(!info.hit) return;
	
	float levels = 1.0 / (1.0 + exp((8.0 - info.pos.z) / 2.0));
	light += levels;
}

void GetAngularIllumination(inout HitStruct info, inout vec3 light, in vec3 normal)
{
	if(!info.hit) return;
	// The higher up the position is, the more light comes from below
	// the lower down the position is, the more light comes from above

	
}

void UI(in vec2 UV, out vec3 col)
{
	vec2 uv = UV;
	uv /= 0.05;
	float t = length(uv);
	t -= 0.20;
	t = abs(t);
	t /= 0.1;

	col = mix(col, vec3(1.0), clamp(1.0 - t, 0.0, 1.0));
}

void main()
{
	vec2 uv = 2.0 * (iCoord - iResolution / 2.0) / iResolution.x;
	vec3 rd = vec3(uv, 1.0).xzy * cam.dirs;
	float exposure = exp(1.0);
	//===========================================================================//
	vec3 col = vec3(0.0);
	HitStruct hitinfo = HitStruct
	(
		false,
		cam.pos,
		ivec3(floor(cam.pos))
	);
	float tdist = 0.0;
	vec3 albedo = vec3(1.0);
	vec3 light = Sky(rd);
	//===========================================================================//
	DDAV2(hitinfo.pos, rd, tdist, hitinfo);
	vec3 uvt = clamp(hitinfo.pos - hitinfo.cell, 0.0, 1.0);

	GetAlbedo(uvt, hitinfo.hit, albedo);
	hitinfo.cell = ivec3(floor(hitinfo.pos));

	HitStruct oldhit = hitinfo;

	vec3 tlight = selectlight * float(hasselection);
	GetSunLight(hitinfo, light, tdist, GetFaceNormal(uvt));
	GetGlobalIllumination(oldhit, light);
	GetAngularIllumination(oldhit, light, GetFaceNormal(uvt));
	light += tlight;

	col = albedo * light;

	col = 1.0 - exp(-col * exposure);

	UI(uv, col);
	//===========================================================================//
	fragcol = vec4(col, 1.0);
}