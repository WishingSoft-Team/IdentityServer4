# 推送修正版 IdentityServer4 到 Azure Artifacts

本文說明如何將 fork 並修改過的 IdentityServer4 套件，
發佈到 Azure DevOps Artifacts，讓原本的系統改用此修正版本。

---

## 前置條件

| 項目 | 說明 |
|------|------|
| Azure DevOps 組織 | 需有 Project 管理員或 Feed 管理員權限 |
| .NET 8 SDK | Build Agent 需安裝 |
| git tag | 用來決定 NuGet 套件版本號 |

---

## 一、建立 Azure Artifacts Feed

1. 進入 Azure DevOps → **Artifacts** → **Create Feed**
2. 設定：
   - **Name**: `IdentityServer4-Internal` (可自訂，需與 pipeline 參數一致)
   - **Visibility**: `Private`
   - **Upstream sources**: 勾選 `Include packages from common public sources`
     (這樣原系統其他套件仍可從 nuget.org 取得)
3. 記下 Feed 的 Source URL，格式為：
   ```
   https://pkgs.dev.azure.com/{organization}/{project}/_packaging/{feedName}/nuget/v3/index.json
   ```

---

## 二、設定 Build Pipeline

1. 在 Azure DevOps → **Pipelines** → **New Pipeline**
2. 選 **Azure Repos Git** (或你的 repo 來源)
3. 選 **Existing Azure Pipelines YAML file**
4. 路徑選 `/azure-pipelines.yml`
5. 儲存 (先不 Run)

### 設定 Environment (手動核准，可選)

若想在推送前讓人工審核：

1. 到 **Pipelines** → **Environments** → **New environment**
2. Name: `nuget-publish`，Resource: None
3. 點進去 → **Approvals and checks** → **Add** → **Approvals**
4. 設定核准人員

若不需要核准，跳過此步驟，pipeline 會自動在 Build 通過後推送。

---

## 三、設定套件版本號 (版本管理策略)

版本號決定邏輯 (優先順序)：

```
手動輸入參數 > git tag > fallback (4.0.0-build.{BuildId})
```

### 方式 A：推送 git tag (建議)

```bash
# 標記此修正版本
git tag 4.1.2-internal.1
git push origin 4.1.2-internal.1
```

Pipeline 會自動觸發並使用 `4.1.2-internal.1` 作為版本號。

### 方式 B：手動執行 Pipeline 並輸入版本

在 Azure DevOps → Run Pipeline → 填入 `packageVersion` 參數。

---

## 四、原系統改用修正版套件

### 4.1 設定 nuget.config

在原系統的 repo 根目錄新增或修改 `nuget.config`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!-- 內部修正版 Feed (優先) -->
    <add key="IdentityServer4-Internal"
         value="https://pkgs.dev.azure.com/{organization}/{project}/_packaging/IdentityServer4-Internal/nuget/v3/index.json" />
    <!-- 公開 nuget.org (其他套件來源) -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <!-- 強制 IdentityServer4 系列套件只從內部 Feed 取得 -->
    <packageSource key="IdentityServer4-Internal">
      <package pattern="IdentityServer4" />
      <package pattern="IdentityServer4.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

> **重要**: `packageSourceMapping` 確保 `IdentityServer4.*` 系列只從內部 Feed 解析，
> 避免意外拉到 nuget.org 上的舊版本。

### 4.2 更新 .csproj 版本參考

```xml
<!-- 原本 -->
<PackageReference Include="IdentityServer4" Version="4.1.0" />

<!-- 改為修正版版本號 -->
<PackageReference Include="IdentityServer4" Version="4.1.2-internal.1" />
```

### 4.3 CI/CD 驗證存取權限

若原系統的 pipeline 需要存取此 Feed，在其 pipeline 加上：

```yaml
- task: NuGetAuthenticate@1
  displayName: 'Authenticate to Azure Artifacts'
```

Azure DevOps 會自動使用 `$(System.AccessToken)` 進行驗證，
確保 **Project Collection Build Service** 帳號對此 Feed 有 `Reader` 權限
(Artifacts → Feed Settings → Permissions)。

---

## 五、各套件對應關係

| 套件名稱 | 專案路徑 |
|---------|---------|
| `IdentityServer4.Storage` | `src/Storage/src/` |
| `IdentityServer4` | `src/IdentityServer4/src/` |
| `IdentityServer4.EntityFramework.Storage` | `src/EntityFramework.Storage/src/` |
| `IdentityServer4.EntityFramework` | `src/EntityFramework/src/` |
| `IdentityServer4.AspNetIdentity` | `src/AspNetIdentity/src/` |

只需依照原系統實際使用的套件，選擇性更新版本參考即可。
通常至少需要 `IdentityServer4` + `IdentityServer4.Storage`。

---

## 六、常見問題

### Q: Build 時出現 `strong naming` 錯誤？
`key.snk` 已在 `src/IdentityServer4/src/IdentityServer4.csproj` 設定，
確保 `key.snk` 存在於 `src/` 目錄下。

### Q: Push 時出現 `409 Conflict`？
代表相同版本號已存在，請更新版本號後重試。

### Q: 原系統 restore 時找不到套件？
確認：
1. `nuget.config` 的 Feed URL 正確
2. Build Service 帳號有 Feed Reader 權限
3. 版本號與 `PackageReference` 完全一致（含 prerelease suffix）
