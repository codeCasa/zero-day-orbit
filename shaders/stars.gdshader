shader_type canvas_item;

uniform float fade_alpha : hint_range(0.0, 1.0) = 0.0;
uniform float time_scale : hint_range(0.1, 5.0) = 1.0;
uniform int star_count : hint_range(50, 500) = 350;
uniform float star_size : hint_range(0.5, 3.0) = 2.2;
uniform float twinkle_speed : hint_range(0.5, 3.0) = 1.5;

// Simple hash function for pseudo-random numbers
float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Generate star brightness with twinkle
float get_star(vec2 uv, float time_offset) {
	vec2 cell = floor(uv);
	vec2 local = fract(uv);
	
	float star_value = 0.0;
	
	// Check neighboring cells for stars
	for(int x = -1; x <= 1; x++) {
		for(int y = -1; y <= 1; y++) {
			vec2 neighbor = cell + vec2(float(x), float(y));
			float random = hash(neighbor);
			
			// Only show stars based on density
			if(random > (1.0 - float(star_count) / 1000.0)) {
				// Star position within cell
				vec2 star_pos = vec2(
					hash(neighbor + vec2(1.0, 0.0)),
					hash(neighbor + vec2(0.0, 1.0))
				);
				
				vec2 offset = neighbor + star_pos - uv;
				float dist = length(offset);
				
				// Star brightness with twinkle
				float base_brightness = hash(neighbor + vec2(2.0, 2.0)) * 0.7 + 0.6;
				float twinkle_offset = hash(neighbor + vec2(3.0, 3.0)) * 6.28;
				float twinkle = sin(TIME * twinkle_speed * time_scale + twinkle_offset) * 0.3 + 0.7;
				
				// Star glow with increased intensity
				float star = exp(-dist * 80.0 / star_size) * base_brightness * twinkle;
				star_value = max(star_value, star);
			}
		}
	}
	
	return star_value;
}

void fragment() {
	vec2 uv = UV * vec2(25.0, 15.0); // Increased scale for more stars
	
	float stars = get_star(uv, TIME * time_scale);
	
	// Create star color with slight blue/white variation and increased brightness
	vec3 star_color = vec3(0.95 + stars * 0.05, 0.95 + stars * 0.05, 1.0);
	
	// Apply fade alpha from C# code with boosted intensity
	COLOR = vec4(star_color * stars * fade_alpha * 1.3, fade_alpha * stars);
}
