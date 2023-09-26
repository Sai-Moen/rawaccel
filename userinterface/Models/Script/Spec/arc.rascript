Arc mode by SaiMoen

[

	Offset := 0;
	Limit := 4;
	Midpoint := 16;

]

	pLimit := Limit - 1;

{

	if (x > Offset):
		x -= Offset;
		y += (pLimit / x) * (x - Midpoint * atan(x / Midpoint));
	:

}