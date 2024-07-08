xvar : byte = 152;
yvar : i64 = -1365153153151351;
zvar : byte = xvar;

fvar : byte* = &zvar;

exit *fvar;