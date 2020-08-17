
using System;
using Gehtsoft.ResourceManager;

namespace Gehtsoft.ResourceManager.Test
{
    public static class Main
    {
    
    public static class group1
    {
    
    public static string message1 => ResourceManager.Messages["group1.message1"];

    
    public static string message2 => ResourceManager.Messages["group1.message2"];

    
    public static string message3(params object[] parameters) { return string.Format(ResourceManager.Messages["group1.message3"], parameters); }
    
    }
    
    public static string message1 => ResourceManager.Messages["message1"];

    
    }
}
    