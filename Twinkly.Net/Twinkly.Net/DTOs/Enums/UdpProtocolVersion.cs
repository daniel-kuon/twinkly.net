namespace Twinkly.Net.DTOs.Enums;

/// <summary>
/// UDP frame protocol version used by Twinkly devices.
/// </summary>
public enum UdpProtocolVersion : byte
{
    /// <summary>
    /// Version 1 - Used in generation I devices.
    /// Header: 1 byte version (0x01) + 8 bytes token + 1 byte LED count.
    /// Body: Frame format, no fragmentation.
    /// </summary>
    Version1 = 0x01,

    /// <summary>
    /// Version 2 - Used in generation II devices until firmware version 2.4.6 (inclusive).
    /// Header: 1 byte version (0x02) + 8 bytes token + 1 byte 0x00.
    /// Body: Movie format, no fragmentation.
    /// </summary>
    Version2 = 0x02,

    /// <summary>
    /// Version 3 - Used in generation II devices from firmware version 2.4.14.
    /// Header: 1 byte version (0x03) + 8 bytes token + 2 bytes 0x00 + 1 byte fragment number.
    /// Body: Frames fragmented into UDP datagrams up to 900 bytes.
    /// </summary>
    Version3 = 0x03
}
