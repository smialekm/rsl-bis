using System.Globalization;
using System.Xml;

public static class Utils {

    private static TextInfo Ti = new CultureInfo("en-UK",false).TextInfo;
    public static string ToPascalCase(string input){
        return Ti.ToTitleCase(input).Replace(" ", "");
    }

    public static string ToCamelCase(string input){
        string output = ToPascalCase(input);
        return output[0..1].ToLower() + output[1..];
    }
}