msg : string = "Message was never set.\n";

if 1
{
    useless : string* = &msg;
    {
        useless2 : string = msg;

        msg = "Wassup\n";
    }
}
elif 0
{
    msg = "No wassup\n";
}
else
{
    msg = "Never wassup :(\n";
}

write msg;