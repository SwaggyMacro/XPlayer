using XPlayer.Desktop.Models;

namespace EasyChat.Services.Languages;

/// <summary>
/// Static helper class to access well-known language definitions.
/// </summary>
public static class LanguageKeys
{
    #region Language ID Constants
    
    public const string ChineseSimplifiedId = "zh-CN";
    public const string EnglishId = "en-US";

    #endregion

    #region Language Definition 
    
    public static LanguageDefinition ChineseSimplified => new(ChineseSimplifiedId, "简体中文", "Chinese (Simplified)", "cn.png");
    public static LanguageDefinition English => new(EnglishId, "英语", "English", "us.png");

    #endregion
}
