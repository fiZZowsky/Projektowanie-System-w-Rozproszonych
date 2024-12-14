using Common.GRPC;

namespace Common.Converters;

public class FormatConverter
{
    public static string SanitizeFileName(string input)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();

        var sanitized = new string(input
                                    .Where(c => !invalidChars.Contains(c))
                                    .ToArray());

        return sanitized;
    }

    public static async Task<FileData> DecodeFileDataFromName(FileInfo fileInfo)
    {
        var fileNameParts = fileInfo.Name.Split('_');

        if (fileNameParts.Length < 5)
        {
            throw new InvalidOperationException("Filename format is incorrect.");
        }

        var fileName = fileNameParts[0];
        var fileType = fileNameParts[1];
        var userId = fileNameParts[2];
        var creationDateString = fileNameParts[3];
        var guid = fileNameParts[4];

        DateTime creationDate = DateTimeConverter.ConvertToDateTime(creationDateString);

        byte[] fileContent = await File.ReadAllBytesAsync(fileInfo.FullName);

        var fileData = new FileData
        {
            FileName = fileName,
            FileContent = Google.Protobuf.ByteString.CopyFrom(fileContent),
            FileType = fileType,
            CreationDate = DateTimeConverter.ConvertToTimestamp(creationDate)
        };

        return fileData;
    }
}
