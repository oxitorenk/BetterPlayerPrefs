using System;
using BetterPlayerPrefs.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace BetterPlayerPrefs.Tests
{
    public class BetterPlayerPrefsTests
    {
        private const string FirstKey = "BetterPlayerPrefs.Tests.Primary";
        private const string SecondKey = "BetterPlayerPrefs.Tests.Other";

        [SetUp, TearDown]
        public void CleanUp()
        {
            PlayerPrefs.DeleteKey(FirstKey);
            PlayerPrefs.DeleteKey(SecondKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void SaveAndLoadRoundTrip()
        {
            var target = new MockPersistent(FirstKey, 3);
            target.Save(new SaveData { number = 7, text = "saved" }, false);

            Assert.IsTrue(target.TryLoad(out SaveData data, out var version));
            Assert.AreEqual(7, data.number);
            Assert.AreEqual("saved", data.text);
            Assert.AreEqual(3, version);
        }

        [Test]
        public void StructRoundTrip()
        {
            var target = new MockPersistent(FirstKey, 1);
            target.Save(default(StructData), false);

            Assert.IsTrue(target.TryLoad(out StructData data, out _));
            Assert.AreEqual(0, data.number);
        }

        [Test]
        public void MissingKeyReturnsFalse()
        {
            var target = new MockPersistent(FirstKey, 1);

            Assert.IsFalse(target.TryLoad(out SaveData data, out var version));
            Assert.IsNull(data);
            Assert.AreEqual(-1, version);
        }

        [TestCase(0), TestCase(-1)]
        public void InvalidSaveVersionThrows(int version)
        {
            var target = new MockPersistent(FirstKey, version);
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                target.Save(new SaveData(), false));
            
            StringAssert.StartsWith("[BetterPlayerPrefs]:", exception.Message);
            Assert.IsFalse(PlayerPrefs.HasKey(FirstKey));
        }

        [TestCase("{", "Failed to deserialize")]
        [TestCase("null", "Failed to deserialize")]
        [TestCase("{\"hasData\":true,\"data\":{\"number\":1}}", "Version missing")]
        [TestCase("{\"version\":1,\"data\":{\"number\":1}}", "Data field missing")]
        public void InvalidRecordThrows(string json, string expectedMessage)
        {
            PlayerPrefs.SetString(FirstKey, json);
            var target = new MockPersistent(FirstKey, 1);
            var exception = Assert.Throws<InvalidOperationException>(() =>
                target.TryLoad<SaveData>(out _, out _));
            
            StringAssert.StartsWith("[BetterPlayerPrefs]:", exception.Message);
            StringAssert.Contains(expectedMessage, exception.Message);
        }

        [Test]
        public void NullTargetThrows()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => 
                BetterPlayerPrefsManager.Save(null, new SaveData(), false));
            StringAssert.StartsWith("[BetterPlayerPrefs]:", exception.Message);
        }

        [TestCase(null), TestCase(""), TestCase("   ")]
        public void InvalidKeyThrows(string key)
        {
            var target = new MockPersistent(key, 1);
            var exception = Assert.Throws<ArgumentException>(() => target.TryLoad<SaveData>(out _, out _));
            
            StringAssert.StartsWith("[BetterPlayerPrefs]:", exception.Message);
        }

        [Test]
        public void NullDataThrowsWithoutWriting()
        {
            var target = new MockPersistent(FirstKey, 1);
            var exception = Assert.Throws<ArgumentNullException>(() => 
                target.Save<SaveData>(null, false));
            
            StringAssert.StartsWith("[BetterPlayerPrefs]:", exception.Message);
            Assert.IsFalse(PlayerPrefs.HasKey(FirstKey));
        }

        [Test]
        public void DeleteRemovesOnlyTargetKey()
        {
            PlayerPrefs.SetString(FirstKey, "delete");
            PlayerPrefs.SetString(SecondKey, "keep");

            new MockPersistent(FirstKey, 1).Delete(false);
            Assert.IsFalse(PlayerPrefs.HasKey(FirstKey));
            Assert.IsTrue(PlayerPrefs.HasKey(SecondKey));
            
            new MockPersistent(SecondKey, 1).Delete(true);
            Assert.IsFalse(PlayerPrefs.HasKey(SecondKey));
        }

        [Test]
        public void VersionMismatchReturnsSavedVersion()
        {
            new MockPersistent(FirstKey, 1).Save(new SaveData(), false);

            Assert.IsTrue(new MockPersistent(FirstKey, 2).TryLoad(out SaveData _, out var savedVersion));
            Assert.AreEqual(1, savedVersion);
        }

        private sealed class MockPersistent : IPersistent
        {
            public string SaveKey { get; }
            public int SaveVersion { get; }

            public MockPersistent(string saveKey, int saveVersion)
            {
                SaveKey = saveKey;
                SaveVersion = saveVersion;
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public int number;
            public string text;
        }

        [Serializable]
        private struct StructData
        {
            public int number;
        }
    }
}
