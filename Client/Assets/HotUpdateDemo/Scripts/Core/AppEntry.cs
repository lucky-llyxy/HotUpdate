using System.Collections;
using HotUpdateDemo.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HotUpdateDemo.Core
{
    public sealed class AppEntry : MonoBehaviour
    {
        private const string LoginPanelAddress = "ui/login_panel";

        [SerializeField] private UpdatePanel updatePanel;
        [SerializeField] private float fakeCheckSeconds = 0.5f;
        [SerializeField] private float fakeDownloadSeconds = 1.2f;

        private AsyncOperationHandle<GameObject> loginPanelHandle;
        private GameObject loginPanelInstance;

        private IEnumerator Start()
        {
            if (updatePanel == null)
            {
                updatePanel = FindFirstObjectByType<UpdatePanel>(FindObjectsInactive.Include);
            }

            if (updatePanel == null)
            {
                Debug.LogError("UpdatePanel is missing. Run HotUpdate Demo/01 Create Bootstrap Scene again.");
                yield break;
            }

            updatePanel.BindEnterButton(EnterGame);
            yield return RunStartupFlow();
        }

        private IEnumerator RunStartupFlow()
        {
            updatePanel.SetButtonVisible(false);
            updatePanel.SetProgress(0f, "准备检查资源更新...");

            yield return new WaitForSeconds(fakeCheckSeconds);
            updatePanel.SetProgress(0.2f, "检查 Remote Catalog...");

            yield return new WaitForSeconds(fakeCheckSeconds);
            updatePanel.SetProgress(0.35f, "计算需要下载的资源大小...");

            var elapsed = 0f;
            while (elapsed < fakeDownloadSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / fakeDownloadSeconds);
                updatePanel.SetProgress(Mathf.Lerp(0.35f, 1f, t), "模拟下载远程资源...");
                yield return null;
            }

            updatePanel.SetProgress(1f, "资源准备完成");
            updatePanel.SetButtonVisible(true);
        }

        private void EnterGame()
        {
            StartCoroutine(LoadLoginPanel());
        }

        private IEnumerator LoadLoginPanel()
        {
            if (loginPanelInstance != null)
            {
                updatePanel.gameObject.SetActive(false);
                yield break;
            }

            updatePanel.SetButtonVisible(false);
            updatePanel.SetProgress(1f, "正在初始化 Addressables...");

            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            if (initHandle.Status != AsyncOperationStatus.Succeeded)
            {
                updatePanel.SetProgress(1f, "Addressables 初始化失败，请看 Console");
                updatePanel.SetButtonVisible(true);
                yield break;
            }

            updatePanel.SetProgress(1f, $"正在加载 {LoginPanelAddress}...");

            var parent = updatePanel.transform.parent;
            loginPanelHandle = Addressables.InstantiateAsync(LoginPanelAddress, parent);
            yield return loginPanelHandle;

            if (loginPanelHandle.Status != AsyncOperationStatus.Succeeded)
            {
                updatePanel.SetProgress(1f, $"加载失败：{LoginPanelAddress}");
                updatePanel.SetButtonVisible(true);
                yield break;
            }

            loginPanelInstance = loginPanelHandle.Result;
            loginPanelInstance.transform.SetAsLastSibling();
            HideUpdatePanels();
            Debug.Log($"Loaded Addressable UI: {LoginPanelAddress}");
        }

        private static void HideUpdatePanels()
        {
            var panels = FindObjectsByType<UpdatePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var panel in panels)
            {
                panel.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (loginPanelHandle.IsValid())
            {
                Addressables.ReleaseInstance(loginPanelHandle);
            }
        }
    }
}
