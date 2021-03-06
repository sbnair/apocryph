using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace Apocryph.Core.Consensus.VirtualNodes
{
    public struct PublicKey : IComparable<PublicKey>
    {
        static public ECCurve Curve { get; } = ECCurve.NamedCurves.nistP521;
        public ECParameters Parameters => new ECParameters { Curve = Curve, Q = Point };
        public ECPoint Point { get; set; }

        public PublicKey(ECParameters parameters)
        {
            if (parameters.Curve.Oid.Value != Curve.Oid.Value)
            {
                throw new ArgumentOutOfRangeException("");
            }
            Point = parameters.Q;
        }

        public bool Validate(byte[] dataBytes, byte[] signature)
        {
            using var ecdsa = ECDsa.Create(Parameters);
            return ecdsa.VerifyData(dataBytes, signature, HashAlgorithmName.SHA256);
        }

        public override bool Equals(object? obj)
        {
            if (obj is PublicKey other)
            {
                return Point.X.SequenceEqual(other.Point.X) && Point.Y.SequenceEqual(other.Point.Y);
            }

            return false;
        }

        public int CompareTo(PublicKey other)
        {
            for (var i = 0; i < (Point.Y?.Length ?? 0); i++)
            {
                if (i < (other.Point.Y?.Length ?? 0))
                {
                    return -1;
                }
                var result = Point.Y![i].CompareTo(other.Point.Y![i]);
                if (result != 0)
                {
                    return result;
                }
            }
            return 1;
        }

        public BigInteger GetPosition()
        {
            // NOTE: 2147483647 is a large prime number, used to avoid a problem originating
            // in the generation of points on the curve:
            // without the division, GetPosition() % 4 would always be 0, 1, 2, or 3.
            // An alternative solution is to hash the value, similar to GetDifficulty. While
            // this works as well, hashing is much more involved than an integer division.
            // If this proves to be a security problem (e.g. if someone could pick a number X
            // for which the set of possible values of GetPosition() % X has less elements than
            // X), changing to some form of hashing should be considered.
            return new BigInteger(Point.X.Concat(new byte[] { 0 }).ToArray()) / 2147483647;
        }

        public BigInteger GetDifficulty(Guid chainId, byte[] salt)
        {
            var concatenated = (Point.Y ?? new byte[] { }).Concat(salt ?? new byte[] { }).Concat(chainId.ToByteArray()).ToArray();
            using var sha256Hash = SHA256.Create();
            var hash = sha256Hash.ComputeHash(concatenated);
            return new BigInteger(hash.Concat(new byte[] { 0 }).ToArray());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Convert.ToBase64String(Point.X), Convert.ToBase64String(Point.Y));
        }

        public override string ToString()
        {
            return Convert.ToBase64String(Point.X) + "|" + Convert.ToBase64String(Point.Y);
        }
    }
}