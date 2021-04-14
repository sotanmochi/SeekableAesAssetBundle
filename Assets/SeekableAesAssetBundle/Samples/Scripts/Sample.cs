namespace SeekableAesAssetBundle.Samples.Scripts
{
    using System;
    using System.IO;
    using System.Text;
    using UnityEngine;
    using UnityEngine.Profiling;
    using UnityEngine.UI;
    using SeekableAesAssetBundle.Scripts;
    using Cysharp.Threading.Tasks;

    public sealed class Sample : MonoBehaviour
    {
        [SerializeField] Image[] _sampleImages = null;
        [SerializeField] Dropdown _dropdownCompressType = null;
        [SerializeField] Toggle _toggleEncrypt = null;

        [SerializeField] Button _buttonLoadFromFile = null;
        [SerializeField] Button _buttonLoadFromMemory = null;
        [SerializeField] Button _buttonLoadFromStream = null;

        readonly string[] Samples = new string[4];

        CompressType CurrentCompressType => (CompressType) _dropdownCompressType.value;
        bool IsEncrypt => _toggleEncrypt.isOn;


        const string OnCompressTypeChangeKey = "OnCompressTypeChangeKey";
        const string OnIsEncryptChangeKey = "OnIsEncryptChangeKey";

        void Start()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < Samples.Length; i++)
            {
                Samples[i] = builder.Append("sample").Append(i + 1).Append(".png").ToString();
                builder.Clear();
            }

            if (PlayerPrefs.HasKey(OnCompressTypeChangeKey))
            {
                this._dropdownCompressType.value = PlayerPrefs.GetInt(OnCompressTypeChangeKey);
            }

            if (PlayerPrefs.HasKey(OnIsEncryptChangeKey))
            {
                this._toggleEncrypt.isOn = PlayerPrefs.GetInt(OnIsEncryptChangeKey) == 1;
            }

            _dropdownCompressType.onValueChanged.AddListener(
                (type) => PlayerPrefs.SetInt(OnCompressTypeChangeKey, type));
            _toggleEncrypt.onValueChanged.AddListener((isOn) => PlayerPrefs.SetInt(OnIsEncryptChangeKey, isOn ? 1 : 0));

            _buttonLoadFromFile.onClick.AddListener(LoadFromFileImpl);
            _buttonLoadFromMemory.onClick.AddListener(LoadFromMemoryImpl);
            _buttonLoadFromStream.onClick.AddListener(LoadFromStreamImpl);
        }

        async void LoadFromFileImpl()
        {
            var path = SamplePathUtility.GetAssetBundlePath(CurrentCompressType, IsEncrypt);
            GC.Collect();
            Profiler.BeginSample($"        >>> LoadFromFile {CurrentCompressType}, {IsEncrypt}");

            if (IsEncrypt)
            {
                Debug.LogWarning("暗号化非対応");
                return;
            }

            var bundle = AssetBundle.LoadFromFile(path);
            await ApplyImages(bundle);

            Profiler.EndSample();
        }

        async void LoadFromMemoryImpl()
        {
            var path = SamplePathUtility.GetAssetBundlePath(CurrentCompressType, IsEncrypt);
            GC.Collect();
            Profiler.BeginSample($"        >>> LoadFromMemory {CurrentCompressType}, {IsEncrypt}");

            byte[] bytes = null;
            bytes = IsEncrypt ? EncryptHelper.DecryptBinary(path) : File.ReadAllBytes(path);
            var bundle = AssetBundle.LoadFromMemory(bytes);
            await ApplyImages(bundle);

            Profiler.EndSample();
        }

        async void LoadFromStreamImpl()
        {
            var path = SamplePathUtility.GetAssetBundlePath(CurrentCompressType, IsEncrypt);
            GC.Collect();
            Profiler.BeginSample($"        >>> LoadFromStream {CurrentCompressType}, {IsEncrypt}");

            if (IsEncrypt)
            {
                using (var reader = new FileStream(path, FileMode.Open))
                using (var cryptStream = new SeekableAesStream(reader, EncryptHelper.Password, EncryptHelper.UniqueSalt))
                {
                    var bundle = AssetBundle.LoadFromStream(cryptStream);
                    await ApplyImages(bundle);
                }
            }
            else
            {
                using (var reader = new FileStream(path, FileMode.Open))
                {
                    var bundle = AssetBundle.LoadFromStream(reader);
                    await ApplyImages(bundle);
                }
            }

            Profiler.EndSample();
        }

        async UniTask ApplyImages(AssetBundle bundle)
        {
            for (var i = 0; i < Samples.Length; i++)
            {
                var request = bundle.LoadAssetAsync<Sprite>(Samples[i]);
                await UniTask.WaitUntil(() => request.isDone);
                _sampleImages[i].sprite = request.asset as Sprite;
            }
        }
    }
}
