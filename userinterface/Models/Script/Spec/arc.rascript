Arc mode by SaiMoen

[

	Offset = 0;
	Limit = 4;
	Midpoint = 16;

]

{

	if (x > Offset):
		x -= Offset;
		y += (Limit / x) * (x - Midpoint * atan(x / Midpoint));
	:

}