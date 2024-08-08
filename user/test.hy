text : string = "This string has not been updated.\n";

condition : bool = not true;

if condition
{
    text = "If condition is true!\n";
}
elif not condition
{
    text = "If condition was false, but the elif condition is true!\n";
}

write text;