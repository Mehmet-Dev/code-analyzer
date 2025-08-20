namespace CodeAnalyzer.Models;

/// <summary>
/// Again, another model used for json output... it collects everything ya know dabadee dabadoo
/// </summary>
public class FileStats
{
    public string FileName { get; set; }
    public int FullLength { get; set; } = 0;
    public int AmountOfMethods { get; set; } = 0;
    public (string name, int lineCount, double averageLength) MethodStats { get; set; }
    public int AmountOfClasses { get; set; }
    public int AmountOfPendingTasks { get; set; } = 0;
    public (int propertyCount, int fieldCount) FieldsAndProperties { get; set; }
    public double CommentDensity { get; set; }

    public FileStats() { }
}
