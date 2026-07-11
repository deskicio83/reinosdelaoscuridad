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
        private const string ICON_STAR = "Assets/Addressables/Art/Common/Star.png";
        private const string ICON_BORDER = "Assets/Addressables/Art/HeroScene/HeroBorderColoured.png";

        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _nombre;
        [SerializeField] private TMP_Text _nivel;
        [SerializeField] private GameObject _favoritoIcon;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private Image _estrellaIcon;
        [SerializeField] private TMP_Text _estrellaTexto;
        [SerializeField] private Image _borderIcon;
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
        private AsyncOperationHandle<Sprite> _starIconHandle;
        private bool _hasStarIconHandle;
        private AsyncOperationHandle<Sprite> _borderIconHandle;
        private bool _hasBorderIconHandle;

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

            if (_estrellaIcon != null)
            {
                Addressables.LoadAssetAsync<Sprite>(ICON_STAR).Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded && _estrellaIcon != null)
                        _estrellaIcon.sprite = handle.Result;
                    _starIconHandle = handle;
                    _hasStarIconHandle = true;
                };
            }

            if (_borderIcon != null)
            {
                Addressables.LoadAssetAsync<Sprite>(ICON_BORDER).Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded && _borderIcon != null)
                        _borderIcon.sprite = handle.Result;
                    _borderIconHandle = handle;
                    _hasBorderIconHandle = true;
                };
            }
        }

        public static Color GetColorForStars(int stars)
        {
            switch (stars)
            {
                case 1: return new Color32(0x9C, 0xA3, 0xAF, 0xFF);
                case 2: return new Color32(0x4A, 0xDE, 0x80, 0xFF);
                case 3: return new Color32(0x38, 0xBD, 0xF8, 0xFF);
                case 4: return new Color32(0xA8, 0x55, 0xF7, 0xFF);
                case 5: return new Color32(0xFB, 0xBF, 0x24, 0xFF);
                default: return new Color32(0xF8, 0x71, 0x71, 0xFF);
            }
        }

        public void Bind(string heroId, string nombre, int nivel, int stars, string portraitAddress, bool favorito,
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
            if (_estrellaTexto != null) _estrellaTexto.text = "x" + stars;
            if (_borderIcon != null) _borderIcon.color = GetColorForStars(stars);

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
            if (_estrellaTexto != null) _estrellaTexto.text = string.Empty;
            if (_borderIcon != null) _borderIcon.color = new Color(1f, 1f, 1f, 0f);

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
            if (_hasStarIconHandle) Addressables.Release(_starIconHandle);
            if (_hasBorderIconHandle) Addressables.Release(_borderIconHandle);
        }

        public Image Portrait => _portrait;
        public TMP_Text Nombre => _nombre;
        public TMP_Text Nivel => _nivel;
        public GameObject FavoritoIcon => _favoritoIcon;
        public GameObject LockIcon => _lockIcon;
        public Button CardButton => _button;
    }
}
