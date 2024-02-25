namespace Adliance.Storyblok.Tests.Extensions;

public static class Thread
{
    public static void DontBombardStoryblokApi()
    {
        System.Threading.Thread.Sleep(10000);
    }
}
