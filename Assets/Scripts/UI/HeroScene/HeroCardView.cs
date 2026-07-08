using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

namespace ReinoOscuridad.UI.HeroScene
{
    public class HeroCardView : MonoBehaviour
    {
        private const string ICON_CORAZON_LLENO = "Assets/Addressables/Art/HeroScene/CorazonLleno.png";
        private const string ICON_CANDADO_CERRADO = "Assets/Addressables/Art/HeroScene/CandadoCerrado.png";

        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _nombre;
        [SerializeField] private TMP_Text _nivel;
        [SerializeField] private GameObject _favoritoIcon;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private Button _button;

        private string _heroId;
        private Action<string> _onClick;
        private int _loadToken;
        private AsyncOperationHandle<Sprite> _portraitHandle;
        private bool _hasPortraitHandle;
        private AsyncOperationHandle<Sprite> _favIconHandle;
        private bool _hasFavIconHandle;
        private AsyncOperationHandle<Sprite> _lockIconHandle;
        private bool _hasLockIconHandle;

        private void Awake()
        {
            var favImg = _favoritoIcon != null ? _favoritoIcon.GetComponent<Image>() : null;
            if (favImg != null)
            {
                Addressables.LoadAssetAsync<Sprite>(ICON_CORAZON_LLENO).Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded && favImg != null)
                        favImg.sprite = handle.Result;
                    _favIconHandle = handle;
                    _hasFavIconHandle = true;
                };
            }

            var lockImg = _lockIcon != null ? _lockIcon.GetComponent<Image>() : null;
            if (lockImg != null)
            {
                Addressables.LoadAssetAsync<Sprite>(ICON_CANDADO_CERRADO).Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded && lockImg != null)
                        lockImg.sprite = handle.Result;
                    _lockIconHandle = handle;
                    _hasLockIconHandle = true;
                };
            }
        }

        public void Bind(string heroId, string nombre, int nivel, string portraitAddress, bool favorito,
            bool bloqueado, Action<string> onClick)
        {
            _heroId = heroId;
            _onClick = onClick;
            _loadToken++;
            int myToken = _loadToken;

            if (_nombre != null) _nombre.text = nombre;
            if (_nivel != null) _nivel.text = "Nv. " + nivel;
            if (_favoritoIcon != null) _favoritoIcon.SetActive(favorito);
            if (_lockIcon != null) _lockIcon.SetActive(bloqueado);

            ReleasePortraitHandle();
            if (_portrait != null) _portrait.sprite = null;

            if (_portrait != null && !string.IsNullOrEmpty(portraitAddress))
            {
                Addressables.LoadAssetAsync<Sprite>(portraitAddress).Completed += handle =>
                {
                    if (myToken != _loadToken) return;
                    if (handle.Status == AsyncOperationStatus.Succeeded && _portrait != null)
                    {
                        _portrait.sprite = handle.Result;
                        _portraitHandle = handle;
                        _hasPortraitHandle = true;
                    }
                };
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _onClick?.Invoke(_heroId));
            }
        }

        public void BindBuySlot(string label, Action onClick)
        {
            _heroId = null;
            _loadToken++;
            ReleasePortraitHandle();

            if (_portrait != null) _portrait.sprite = null;
            if (_nombre != null) _nombre.text = label;
            if (_nivel != null) _nivel.text = string.Empty;
            if (_favoritoIcon != null) _favoritoIcon.SetActive(false);
            if (_lockIcon != null) _lockIcon.SetActive(false);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onClick?.Invoke());
            }
        }

        private void ReleasePortraitHandle()
        {
            if (_hasPortraitHandle)
            {
                Addressables.Release(_portraitHandle);
                _hasPortraitHandle = false;
            }
        }

        private void OnDestroy()
        {
            ReleasePortraitHandle();
            if (_hasFavIconHandle) Addressables.Release(_favIconHandle);
            if (_hasLockIconHandle) Addressables.Release(_lockIconHandle);
        }

        public Image Portrait => _portrait;
        public TMP_Text Nombre => _nombre;
        public TMP_Text Nivel => _nivel;
        public GameObject FavoritoIcon => _favoritoIcon;
        public GameObject LockIcon => _lockIcon;
        public Button CardButton => _button;
    }
}
