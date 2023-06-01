Example RawAccelScript using the Arc formula.

[

	Input.Offset = 0;
	Limit = 4;
	Midpoint = 16;

]

NewX = 0;
y1 = 0;
y2 = 0;

{

	if (x > Input.Offset):
		NewX = x - Input.Offset;
		y1 = Limit / NewX;
		y2 = NewX - Midpoint * atan(NewX / Midpoint);
	:

	y += y1 * y2;

}