finalErrorCode : i64 = ((5 + 64) / 2) * 3;
actualErrorCode : u64 = cast u64 finalErrorCode;

{
    x : u64 = cast u64 15;

    y : i64 = (cast i64 x) - 16;
}

actualErrorCode = actualErrorCode * cast u64 2;

exit actualErrorCode;