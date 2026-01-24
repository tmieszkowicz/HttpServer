using System.Text;

namespace Internal.Headers;

public class HttpHeader
{
    public Dictionary<string, string> Headers { get; private set; } = [];

    public (int bytesRead, bool done) Parse(ReadOnlySpan<byte> buffer)
    {
        ReadOnlySpan<byte> SEPARATOR = "\r\n"u8;
        int bytesRead = 0;
        bool isDone = false;

        while (true)
        {
            ReadOnlySpan<byte> currentBuffer = buffer[bytesRead..];
            int separatorIndex = currentBuffer.IndexOf(SEPARATOR);

            if (separatorIndex == -1)
            {
                break;
            }

            if (separatorIndex == 0)
            {
                isDone = true;
                bytesRead += SEPARATOR.Length;
                break;
            }

            ReadOnlySpan<byte> headerLine = currentBuffer[..separatorIndex];

            if (!ParseHeader(headerLine))
            {
                return (-1, false);
            }

            bytesRead += separatorIndex + SEPARATOR.Length;
        }

        return (bytesRead, isDone);
    }

    private bool ParseHeader(ReadOnlySpan<byte> buffer)
    {
        int colonIndex = buffer.IndexOf((byte)':');
        if (colonIndex == -1) return false;

        int valueStart = colonIndex + 1;
        if (valueStart >= buffer.Length) return false;

        int valueIndex = valueStart + buffer[valueStart..].IndexOfAnyExcept((byte)' ');
        if (valueIndex == -1) return false;

        ReadOnlySpan<byte> name = buffer[..colonIndex];
        ReadOnlySpan<byte> value = buffer[valueIndex..].Trim((byte)' ');

        if (!IsToken(name)) return false;

        string nameString = Encoding.ASCII.GetString(name);
        string valueString = Encoding.ASCII.GetString(value);

        Set(nameString, valueString);

        return true;
    }

    public int GetContentLength()
    {
        if (Headers.TryGetValue("content-length", out string? value)
            && int.TryParse(value, out int length))
            return length;
        return 0;
    }

    private static bool IsToken(ReadOnlySpan<byte> str)
    {
        foreach (byte ch in str)
        {
            bool found = false;

            if ((ch >= 'A' && ch <= 'Z') ||
                (ch >= 'a' && ch <= 'z') ||
                (ch >= '0' && ch <= '9'))
            {
                found = true;
            }

            switch (ch)
            {
                case (byte)'!':
                case (byte)'#':
                case (byte)'$':
                case (byte)'%':
                case (byte)'&':
                case (byte)'\'':
                case (byte)'*':
                case (byte)'+':
                case (byte)'-':
                case (byte)'.':
                case (byte)'^':
                case (byte)'_':
                case (byte)'`':
                case (byte)'|':
                case (byte)'~':
                    found = true;
                    break;
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    public void Set(string name, string value)
    {
        name = name.ToLower();
        if (Headers.TryGetValue(name, out string? existing))
        {
            Headers[name] = $"{existing}, {value}";
        }
        else
        {
            Headers.Add(name, value);
        }
    }

    public void Replace(string name, string value)
    {
        name = name.ToLower();
        Headers[name] = value;
    }

    public void Delete(string name)
    {
        name = name.ToLower();
        Headers.Remove(name);
    }
}
