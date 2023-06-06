Test script

[

	Input.Offset = 0;
	Cappucino = 4;
	Midpoint = 16;

]

	NewX = 0;
	y1 = 0;
	y2 = 0;

	four = 4;
	Coffee = Cappucino;

{

	if (x * four > zero + (four + Input.Offset) ^ four ^ Cappucino):
		NewX = x - Input.Offset;
		y1 = Cappucino / NewX;
		y2 = NewX - Midpoint * atan(NewX / Midpoint);
	:

	Coffee ^= four % y2 - y1 * NewX;

	y += y1 * y2;

}