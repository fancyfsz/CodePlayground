using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

public class Jwt
{
    private static readonly Encoding _encoding = Encoding.UTF8;

    public static string Encode(IDictionary<string, object> payload, string key, JwtHashAlgorithm algorithm)
    {
        var header = new Dictionary<string, object>
        {
            { "alg", algorithm.ToString().ToUpper() },
            { "typ", "JWT" }
        };

        var segments = new List<string>
        {
            Base64UrlEncode(JsonConvert.SerializeObject(header, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })),
            Base64UrlEncode(JsonConvert.SerializeObject(payload, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }))
        };

        var stringToSign = string.Join(".", segments.ToArray());
        var bytesToSign = _encoding.GetBytes(stringToSign);
        var keyBytes = _encoding.GetBytes(key);

        using (var hmac = GetHashAlgorithm(algorithm, keyBytes))
        {
            var signature = Base64UrlEncode(hmac.ComputeHash(bytesToSign));
            segments.Add(signature);
        }

        return string.Join(".", segments.ToArray());
    }

    private static HashAlgorithm GetHashAlgorithm(JwtHashAlgorithm algorithm, byte[] keyBytes)
    {
        switch (algorithm)
        {
            case JwtHashAlgorithm.HS256:
                return new HMACSHA256(keyBytes);
            case JwtHashAlgorithm.HS384:
                return new HMACSHA384(keyBytes);
            case JwtHashAlgorithm.HS512:
                return new HMACSHA512(keyBytes);
            default:
                throw new ArgumentException($"Unsupported hash algorithm: {algorithm}");
        }
    }

    private static string Base64UrlEncode(string input)
    {
        var inputBytes = _encoding.GetBytes(input);
        return Base64UrlEncode(inputBytes);
    }

    private static string Base64UrlEncode(byte[] inputBytes)
    {
        var base64 = Convert.ToBase64String(inputBytes);
        var base64Url = base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return base64Url;
    }
}

public enum JwtHashAlgorithm
{
    HS256,
    HS384,
    HS512
}