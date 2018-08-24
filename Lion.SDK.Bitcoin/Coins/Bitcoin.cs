using System;

public class Bitcoin
{
    public static bool IsAddress(string _text)
    {
        try
        {
            Lion.Encrypt.Base58.Decode(_text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}