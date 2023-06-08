Arc mode by SaiMoen

[

	Offset := 0;
	Limit := 4;
	Midpoint := 16;

]

	limit := Limit - 1;

{

	if (x > Offset):
		x -= Offset;
		y += (limit / x) * (x - Midpoint * atan(x / Midpoint));
	:

}