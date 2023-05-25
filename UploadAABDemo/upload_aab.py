import os
from google.oauth2 import service_account
from googleapiclient.discovery import build

# 定义要上传的AAB包的文件路径和应用包名
aab_file_path = '/path/to/your/app.aab'
package_name = 'your.app.package'

# 载入凭据文件
credentials = service_account.Credentials.from_service_account_file('credentials.json')

# 创建Google Play Developer API客户端
service = build('androidpublisher', 'v3', credentials=credentials)

# 创建一个内部应用发布的编辑
edit_request = service.edits().insert(body={}, packageName=package_name)
edit_response = edit_request.execute()
edit_id = edit_response['id']

try:
    # 上传AAB包
    bundle_response = service.edits().bundles().upload(
        editId=edit_id,
        packageName=package_name,
        media_body=aab_file_path,
        media_mime_type='application/octet-stream'
    ).execute()

    # 发布应用
    commit_request = service.edits().commit(
        editId=edit_id,
        packageName=package_name
    )
    commit_response = commit_request.execute()

    print('AAB上传成功！')
    print('应用已发布到内部测试或生产环境。')
    print('应用链接：', commit_response['id'])
except Exception as e:
    print('AAB上传失败:', str(e))

# 删除编辑
service.edits().delete(editId=edit_id, packageName=package_name).execute()

