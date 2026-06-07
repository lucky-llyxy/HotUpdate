using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class HotUpdateSystem : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(HotUpdate());
    }

    private IEnumerator HotUpdate()
    {
        //初始化
        yield return Addressables.InitializeAsync();
        //检查目录更新
        yield return CheckForCatalogUpdates();
        //下载最新目录
        yield return new WaitForSeconds(1f);
        //下载资源
        yield return new WaitForSeconds(1f);
    }

    //检查目录更新（检查服务器上的 Addressables 资源索引表有没有新版本）
    private IEnumerator CheckForCatalogUpdates()
    {
        AsyncOperationHandle<List<string>> checkForCatalogUpdatesHandle = Addressables.CheckForCatalogUpdates();
        yield return checkForCatalogUpdatesHandle;
        if (checkForCatalogUpdatesHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("CheckForCatalogUpdates成功");
            List<string> catalogResult = checkForCatalogUpdatesHandle.Result;
            if (catalogResult.Count > 0)
            {
                for (int i = 0; i < catalogResult.Count; i++)
                {
                    Debug.Log(catalogResult[i]);
                }

                yield return UpdateCatalog(catalogResult);
            }
            else
            {
                Debug.Log("无需更新");
            }
        }
        else
        {
            Debug.LogError($"CheckForCatalogUpdates失败:{checkForCatalogUpdatesHandle.OperationException}");
        }

        Addressables.Release(checkForCatalogUpdatesHandle);
    }

    private IEnumerator UpdateCatalog(List<string> catalogResult)
    {
        AsyncOperationHandle<List<IResourceLocator>> updateCatalogsHandle = Addressables.UpdateCatalogs(catalogResult, false);
        yield return updateCatalogsHandle;
        if (updateCatalogsHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("UpdateCatalogs成功");
            List<IResourceLocator> locators = updateCatalogsHandle.Result;
            if (locators.Count > 0)
            {
                List<object> downloadKeys = new List<object>(1000);
                for (int i = 0; i < locators.Count; i++)
                {
                    Debug.Log(locators[i].LocatorId);
                    downloadKeys.AddRange(locators[i].Keys);
                }
                //TODO:下载资源
                yield return DownloadAssets(downloadKeys);
            }
        }
        else
        {
            Debug.LogError($"UpdateCatalogs失败:{updateCatalogsHandle.OperationException}");
        }

        Addressables.Release(updateCatalogsHandle);
    }
    
    private IEnumerator DownloadAssets(List<object> downloadKeys)
    {
        for (int i = 0; i < downloadKeys.Count; i++)
        {
            Debug.Log(downloadKeys[i]);
        }
        yield return null;
    }
}