using System;

public class SeedManager
{
    public static int CreateRandomSeed()
    {
        Random random = new Random();
        return random.Next();
    }
}