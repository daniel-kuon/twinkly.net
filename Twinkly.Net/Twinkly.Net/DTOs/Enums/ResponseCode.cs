namespace Twinkly.Net.DTOs.Enums;

public enum ResponseCode
{
    /// <summary>
    /// Ok
    /// </summary>
    Ok = 1000,

    /// <summary>
    /// Error
    /// </summary>
    Error = 1001,

    /// <summary>
    /// Invalid argument value
    /// </summary>
    InvalidArgumentValue = 1101,

    /// <summary>
    /// Error
    /// </summary>
    Error1102 = 1102,

    /// <summary>
    /// Error - value too long? Or missing required object key?
    /// </summary>
    ErrorValueTooLongOrMissingRequiredObjectKey = 1103,

    /// <summary>
    /// Error - malformed JSON on input?
    /// </summary>
    ErrorMalformedJsonOnInput = 1104,

    /// <summary>
    /// Invalid Argument Key
    /// </summary>
    InvalidArgumentKey = 1105,

    /// <summary>
    /// Ok?
    /// </summary>
    Ok1107 = 1107,

    /// <summary>
    /// Ok?
    /// </summary>
    Ok1108 = 1108,

    /// <summary>
    /// Error with firmware upgrade - SHA1SUM does not match
    /// </summary>
    FirmwareUpgradeError = 1205
}
