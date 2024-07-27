// title If's and conditions
// expects 0

text : string = "This string has not been updated.\n";

condition : bool = true;

if condition
{
    text = "If condition is true!\n";
}
elif false
{
    text = "If condition was false, but the elif condition is true!\n";
}

write text;