using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Widebrain.Common;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

public class SceneBase : MonoSingleton<SceneBase>
{
    public Widebrain.Enums.eLanguage eLanguage;
    public BottomMenuController menuController;
    public List<c_Display> listDisplay = new List<c_Display>();
    public bool isPopupOpen = false;
    public bool isLoadComplete = false;
    public c_Userinfo userinfo = new c_Userinfo();
    public c_UserSetting userSetting = new c_UserSetting();
    public c_Business_Card userBusinessCard = new c_Business_Card();
    bool isDataUploaded = false;
    public Signal signal;
    public AudioSource BGMAudioSource;
    public AudioSource SFXAudioSource;
    [Header("------------------RootPopup------------------")]
    [SerializeField] private Transform rootPopup;


    private Dictionary<string, WBPopup> popuplist = new Dictionary<string, WBPopup>();

    IEnumerator Start() 
    {
        //로컬라이제이션 세팅
        yield return LocalizationSettings.InitializationOperation;
        if(PlayerPrefs.HasKey(Widebrain.Enums.ePlayerPrefs.Language.ToString())== false)
            PlayerPrefs.SetString(Widebrain.Enums.ePlayerPrefs.Language.ToString(),Widebrain.Enums.eLanguage.En.ToString());
        

        if (Enum.TryParse(PlayerPrefs.GetString(Widebrain.Enums.ePlayerPrefs.Language.ToString()), out Widebrain.Enums.eLanguage enumValue))
        {
            eLanguage = enumValue;
        }
        else
        {
            Debug.Log("Language Setting Error");
        }

        var currentLocale = new Locale();
        if (eLanguage == Widebrain.Enums.eLanguage.En)
            currentLocale = LocalizationSettings.AvailableLocales.Locales[0]; 
        else if (eLanguage == Widebrain.Enums.eLanguage.Kr)
            currentLocale = LocalizationSettings.AvailableLocales.Locales[1]; 

        LocalizationSettings.SelectedLocale = currentLocale;

        menuController.HideBottomMenu();
        //waiting until get Data
        GetDisplay();
    
       
        
        yield return new WaitUntil(() => isDataUploaded == true);
        SceneBase.Instance.MoveScene(Widebrain.Strings.SCENE_TITLE);

    }

    public Transform GetRootPopup()
    {
        return rootPopup.transform;
    }

#region Scene
    public void MoveScene(string SceneName, Action callback = null)
    {
        StartCoroutine(IEMoveScene(SceneName,callback));
    }
    IEnumerator IEMoveScene(string SceneName, Action callback = null)
    {
        //imgMoveSceneBG.color = new Color(0, 0, 0, 0);
        //imgMoveSceneBG.DOFade(1, 1f);

        float delayBeforeLoadScene = 0.5f;
        yield return new WaitForSeconds(delayBeforeLoadScene);

        yield return SceneManager.LoadSceneAsync(SceneName);

        //imgMoveSceneBG.DOFade(0, 1f);
        float delayTime = 1.7f;
        yield return new WaitForSeconds(delayTime);
        callback?.Invoke();

    }
#endregion
    
    #region Popup
    public T AddPopupAsLastSibling<T>(object _data = null) where T : WBPopup
    {

        var obj = AddPopup<T>(rootPopup, _data);
        obj.transform.SetAsLastSibling();

        return obj;
    }
    public T AddPopup<T>(Transform _root, object _data = null) where T : WBPopup
    {
        //CloseAllPopup();
        string popupname = typeof(T).ToString();
        WBPopup data = null;
        if (popuplist.ContainsKey(popupname))
        {
            data = popuplist[popupname];
            data.transform.localScale = Vector3.one;
            data.transform.localPosition = Vector2.zero;
            data.gameObject.SetActive(true);
            data.SetData(_data);

        }
        else
        {
            data = Instantiate(Resources.Load<WBPopup>("Popup/" + popupname), _root) as WBPopup;
            data.name = popupname;
            data.transform.localScale = Vector3.one;
            data.transform.localPosition = Vector2.zero;
            data.SetData(_data);
            data.gameObject.SetActive(true);
            popuplist.Add(popupname, data);
        }
        // AnimationUtil.PopupAlphaIn(data.gameObject, null, 0.3f);
        return data as T;
    }
    public void CloseAllPopup()
    {
        if (rootPopup != null && rootPopup.childCount > 0)
        {
            for (int i = 0; i < rootPopup.childCount; i++)
            {
                var obj = rootPopup.GetChild(i);
                if(obj.gameObject.name.Equals("PopupVideoCall")||obj.gameObject.name.Equals("PopupConferenceCall"))
                    continue;
                else
                    obj.gameObject.SetActive(false);
            }
        }

    }

    public void ClosePopupConference()
    {
        for (int i = 0; i < rootPopup.childCount; i++)
        {
            if (rootPopup.GetChild(i).TryGetComponent<PopupConferenceCall>(out PopupConferenceCall result))
            {
                string popupname = typeof(PopupConferenceCall).ToString();
                Destroy(result.gameObject);
                popuplist.Remove(popupname);
            }

        }

    }

    public void CloseAllPopupAcceptMap()
    {
        if (rootPopup != null && rootPopup.childCount > 0)
        {
            for (int i = 0; i < rootPopup.childCount; i++)
            {
                var obj = rootPopup.GetChild(i);
                if (obj.GetComponent<PopupMap>() == null)
                    obj.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region Localization
    public string GetLocaleString(Widebrain.Enums.eLocaleKey _strKey)
    {
        string result = "";
        int index = 0;
        if (eLanguage == Widebrain.Enums.eLanguage.En)
            index = 0;
        else if (eLanguage == Widebrain.Enums.eLanguage.Kr)
            index = 1;

        Locale currentLocale = LocalizationSettings.AvailableLocales.Locales[index];
        result = LocalizationSettings.StringDatabase.GetLocalizedString("UITable",_strKey.ToString(),currentLocale);
        return result;
    }
    public void SetTextByGetLocaleString(Widebrain.Enums.eLocaleKey _strKey,Text txt)
    {
        string result = "";
        int index = 0;
        if (eLanguage == Widebrain.Enums.eLanguage.En)
            index = 0;
        else if (eLanguage == Widebrain.Enums.eLanguage.Kr)
            index = 1;

        Locale currentLocale = LocalizationSettings.AvailableLocales.Locales[index];

        result = LocalizationSettings.StringDatabase.GetLocalizedString("UITable",_strKey.ToString(),currentLocale);
        txt.text = result;
    }
    #endregion

    #region ReadLocalJSONData
    public void ReadTextData(string dataName, Action<string> _ACTION_READ_LINE)
    {
        /**********************************************************************************/
        TextAsset targetFile = Resources.Load<TextAsset>(string.Format("Data/{0}", dataName));
        /// �н� ���� 
        string jsonFile = targetFile.text;

        if (string.IsNullOrEmpty(jsonFile))
        {
            return;
        }

        _ACTION_READ_LINE(jsonFile);

    }

    public string SetDataToJSON<T>(T data)
    {
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        return json;
    }

    public T SetDataToReadData<T>(string receivedData)
    {
        List<T> t = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(receivedData);
        return t[0];
    }
    #endregion

    #region API
    public void PostData(string _Url,string json,Action<string> complete = null, Action<string> onError = null,string authorization = null)
    {
        StartCoroutine(CoPostData(_Url,json, complete, onError,authorization));
    }


    IEnumerator CoPostData(string url, string json, Action<string> complete = null, Action<string> onError = null,string authorization = null)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(url, json))
        {
            www.SetRequestHeader("Content-type", "application/json");
            www.SetRequestHeader("authorization","bearer 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");
             if(string.IsNullOrEmpty(authorization) == false)
                 www.SetRequestHeader("authorization",authorization);
            byte[] jsonToSend = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                complete?.Invoke(www.downloadHandler.text);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("@@@@@@@@@@@  url " + url + " :  " + www.error);
#endif
                string result = www.error;
                if(string.IsNullOrEmpty(www.downloadHandler.text)== false)
                    result += ","+www.downloadHandler.text;            
                onError?.Invoke(result);
            }

            www.Dispose();
        }
    }

        public void DeleteData(string _Url, Action complete = null, Action<string> onError = null)
    {
        StartCoroutine(CoDeleteData(_Url, complete));
    }


    IEnumerator CoDeleteData(string url, Action complete = null, Action<string> onError = null)
    {
        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            var operation = www.SendWebRequest();

            yield return operation;

            if (www.result == UnityWebRequest.Result.Success)
            {
                complete?.Invoke();
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("@@@@@@@@@@@  url " + url + " :  " + www.error);
#endif
                string result = www.error;
                if(string.IsNullOrEmpty(www.downloadHandler.text)== false)
                    result += ","+www.downloadHandler.text;            
                onError?.Invoke(result);
            }
            www.Dispose();
        }
    }

    public void PutData(string _Url, string json, Action<string> complete = null, Action<string> onError = null)
    {
        StartCoroutine(CoPutData(_Url, json));
    }

    IEnumerator CoPutData(string url, string json, Action<string> complete = null, Action<string> onError = null)
    {
        using (UnityWebRequest www = UnityWebRequest.Put(url, json))
        {
            www.SetRequestHeader("Content-type", "application/json");
            www.SetRequestHeader("Access-Control-Allow-Origin", "*");

            byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                complete?.Invoke(www.downloadHandler.text);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("@@@@@@@@@@@  url " + url + " :  " + www.error);
#endif
               string result = www.error;
                if(string.IsNullOrEmpty(www.downloadHandler.text)== false)
                    result += ","+www.downloadHandler.text;            
                onError?.Invoke(result);
                
            }

            www.Dispose();
        }
    }

    public void GetData(string _Url, System.Action<string> callback, Action<string> onError = null)
    {
        StartCoroutine(CoGetData(_Url, callback,onError));
    }

    public IEnumerator CoGetData(string url, System.Action<string> callback, Action<string> onError = null)
    {
        
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("Content-type", "application/json");
            www.SetRequestHeader("authorization","bearer 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");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                callback?.Invoke(json);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("@@@@@@@@@@@  url " + url + " :  " + www.error);
#endif
                onError?.Invoke(www.downloadHandler.text);
            }

            www.Dispose();
        }
    }

    public void GetVersion(string _Url, System.Action<string> callback, Action<string> onError = null)
    {
        StartCoroutine(CoGetVersion(_Url, callback,onError));
    }

    public IEnumerator CoGetVersion(string url, System.Action<string> callback, Action<string> onError = null)
    {
        
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                callback?.Invoke(json);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("@@@@@@@@@@@  url " + url + " :  " + www.error);
#endif
                onError?.Invoke(www.downloadHandler.text);
            }

            www.Dispose();
        }
    }

    public IEnumerator GetDownloadTexture(string url, Action<Texture> complete)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            www.SetRequestHeader("Content-type", "application/json");
            www.SetRequestHeader("Access-Control-Allow-Origin", "*");
            www.SetRequestHeader("Accept-Encoding", "binary");
            //www.SetRequestHeader("authorization","bearer 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");
            yield return www.SendWebRequest();

            if (www.error == null)
            {
                try
                {
                    var texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    complete?.Invoke(texture);
                }
                catch (ArgumentException e)
                {
#if UNITY_EDITOR
                    Debug.Log("@@@@@@@@@@@  url " + url + " :  " + e.Message);
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("@@@@@@@@@@@  url " + url + " :  " + www.error);
#endif
            }            

            www.Dispose();
        }
    }

    #endregion

    public bool IsEmailValid(string email)
    {
        // 이메일 형식을 검사하는 정규식 패턴
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        // 정규식 패턴에 맞는지 확인
        bool isValid = Regex.IsMatch(email, pattern);

        return isValid;
    }

    void GetDisplay()
    {
        GetData(Widebrain.Strings.GET_DISPLAY_ALL,o=>
        {
            listDisplay = Newtonsoft.Json.JsonConvert.DeserializeObject<List<c_Display>>(o);
            isDataUploaded = true;
        },e=>
        {
            Debug.Log("ERROR  "+ e);
            isDataUploaded = true;
        });
        
    }


    public void PlaySFX()
    {
        if(SceneBase.Instance.userSetting.systemVolumeSettingValue.Equals("0"))
            return;

        float value = float.Parse(SceneBase.Instance.userSetting.systemVolumeSettingValue)/100;
        SceneBase.Instance.SFXAudioSource.volume = value;
        SceneBase.Instance.SFXAudioSource.Play();
    }

    
}