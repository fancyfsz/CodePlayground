403 Forbidden **是 Jenkins 认证失败 或 用户权限不足** 的典型错误。
 我们来逐步排查，让你最快定位原因。

------

# ✅ 先确认三件关键事（99% 的 403 原因都在这里）

## **① Jenkins 是否启用了 API Token 访问？**

如果你用的是 **密码**，某些 Jenkins 版本不允许密码触发远程构建 → 直接 403。

建议改用 API Token：

Jenkins → 右上角用户头像 → **Configure** → API Token → 新建 Token。

------

## **② 用户是否有权限构建该 Job？**

Jenkins 全局安全配置：

*Manage Jenkins → Configure Global Security → Authorization*

如果是 **Matrix-based security**
 必须确保：

```
User "admin"   -> Job Build 权限（Build / Configure / Read / Workspace）
```

缺一个都会 403。

------

## **③ Jenkins Job 是否关闭了“允许远程触发”？（重要）**

进入具体的 Job：
 `http://<jenkins>/job/<jobName>/configure`

查看是否启用：

### ✔ 旧版本 Jenkins

**"Trigger builds remotely (e.g., from scripts)"**
 勾选后有一个 Auth Token。

### ✔ 新版本 Jenkins

这个选项隐藏，但远程触发仍然必须满足权限 + POST + crumb。

------

# ⭐ 最关键：你必须确认 Jenkins 是否启用了 “防止CSRF” (`Crumb`)

如果开启了（默认是开），直接 POST 会 403。

执行：

```
http://<jenkins>/crumbIssuer/api/json
```

看看能不能访问。

如果能访问，会返回：

```json
{
  "_class":"...DefaultCrumbIssuer",
  "crumb":"xxxxxx",
  "crumbRequestField":"Jenkins-Crumb"
}
```

然后脚本必须携带这个 crumb。



# ❗我需要你告诉我访问这个地址后看到的 **实际返回内容**：

请在浏览器里打开：

```html
http://your_url(替换成你实际的Jenkins地址):8080/crumbIssuer/api/json
```

然后把看到的结果贴给我（可能是以下几种之一）：

------

### **① 返回 JSON（类似）：**

```json
{
  "crumbRequestField": "Jenkins-Crumb",
  "crumb": "abcdef123456..."
}
```

→ 说明 CSRF (Crumb) 开启，需要脚本带 crumb 才能触发构建。

------

### **② 返回 404 或 Cannot find…**

→ 表示 Jenkins 的 CrumbIssuer 闭了，不用 crumb。

------

### **③ 跳到登录页**

→ 说明认证失败（用户名/密码/Token 不对）。

------

### **④ 返回 403 Forbidden**

→ 表示你访问 crumb 接口也没权限 → 权限配置问题（Job 权限 / Crumb 权限）。

