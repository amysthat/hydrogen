x : i64 = 64 - 64 * 2;

y : i64 = x * -1;

{
	z : u64 = 10;
	t : u64 = z * 5;
}

return_code : u64 = cast u64 y;

exit return_code;
