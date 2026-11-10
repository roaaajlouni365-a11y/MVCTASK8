# تشغيل مشروع CafeteriaSupplyTask8

مشروع ASP.NET Core MVC لإدارة طلبات توريد كافتيريا الجامعة، مبني على مشروعك الأصلي بنفس الاسم وnamespace. الواجهة باللغة الإنجليزية لتطابق أسماء المتطلبات، وهذا الدليل بالعربي.

## 1. المتطلبات

- Windows وVisual Studio 2026 (الإصدار 18.0 أو أحدث) مع workload: **ASP.NET and web development**.
- .NET 10 SDK. المشروع يستهدف `net10.0` ويستخدم حزم Microsoft إصدار `10.0.14`.
- SQL Server Express LocalDB. ثبّته من Visual Studio Installer → Modify → Individual components → SQL Server Express LocalDB.
- إنترنت أول مرة لاستعادة حزم NuGet.

إذا لديك Visual Studio 2022 فقط، حدّث إلى Visual Studio 2026 لتشغيل هذا المشروع كما هو؛ لا تغيّر TargetFramework وحده.

مرجع توافق .NET وVisual Studio: https://learn.microsoft.com/en-us/dotnet/core/install/windows

## 2. افتح المشروع

1. فك ضغط ZIP بالكامل باستخدام Extract All؛ لا تفتح المشروع من داخل الملف المضغوط.
2. افتح `CafeteriaSupplyTask8.slnx` في Visual Studio.
3. اضغط يمين على مشروع **CafeteriaSupplyTask8** → **Set as Startup Project**. المشروع الآخر `CafeteriaSupplyTask8.Checks` للفحوص فقط.
4. انتظر استعادة NuGet Packages. إذا لم تتم: اضغط يمين على Solution → Restore NuGet Packages.
5. اختر Build → Build Solution.

## 3. حدّد حساب SuperAdmin لأول تشغيل

لا توجد كلمة مرور ثابتة محفوظة في الكود. أنت تختار حساب الإدارة الأول.

1. اضغط يمين على مشروع **CafeteriaSupplyTask8** → **Manage User Secrets**.
2. ضع الإعدادات التالية في `secrets.json`، أو ادمج قسم Bootstrap إذا كان الملف يحتوي إعدادات أخرى:

```json
{
  "Bootstrap": {
    "Email": "admin@cafeteria.local",
    "Password": "ChooseYourPassword!2026",
    "FullName": "Cafeteria Super Admin"
  }
}
```

3. غيّر كلمة المرور التجريبية إلى كلمة تختارها. المطلوب 8 أحرف على الأقل، حرف كبير وصغير ورقم ورمز. احفظ الملف.

سيُنشأ هذا الحساب بدور SuperAdmin فقط عندما لا يوجد أي SuperAdmin. لا تتم ترقية حساب مسجل مسبقًا تلقائيًا؛ استخدم بريدًا جديدًا عند التهيئة. بعد إنشاء الحساب، تغيير Bootstrap لا يغيّر كلمة مروره. يمكنك حذف قسم Bootstrap من User Secrets بعد نجاح التشغيل الأول.

التسجيل من صفحة Register يمنح **User فقط** مهما كانت القيم المرسلة. تغيير الدور لاحقًا مسموح من صفحة Users لحساب SuperAdmin.

## 4. إعداد قاعدة البيانات

احتفظنا باسم قاعدة البيانات الموجود في مشروعك الأصلي. الاتصال في `CafeteriaSupplyTask8/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=aspnet-CafeteriaSupplyTask8-43087c40-50cc-4a31-9e20-a686745c4921;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

لا تحتاج لإنشاء الجداول يدويًا: أثناء التشغيل بملف التعريف المرفق (Development)، يطبق البرنامج الـ Migrations تلقائيًا، ثم ينشئ الأدوار والحساب الأول و6 منتجات تجريبية عند خلو الكتالوج والطلبات.

يوجد Migration القالب الأصلي `CreateIdentitySchema` ثم `CompleteCafeteria`. الجديد يضيف FullName إلى AspNetUsers ويضيف Products وSupplyRequests وRequestItems؛ لا ينشئ جداول Users أو Roles بديلة ولا يحذف جداولك السابقة.

إذا أردت تنفيذ تحديث قاعدة البيانات بنفسك قبل التشغيل، افتح Tools → NuGet Package Manager → Package Manager Console، واجعل Default project هو **CafeteriaSupplyTask8**، ثم:

```powershell
Update-Database
```

لا تنفّذ Add-Migration لأول تشغيل؛ الـ Migrations جاهزة. إذا أضفت خصائص مستقبلًا، تستخدم Add-Migration باسم جديد ثم Update-Database.

`Schema.sql` نسخة SQL قابلة لإعادة التنفيذ بحسب سجل migrations، للاطلاع أو التطبيق اليدوي على قاعدة بيانات تختارها؛ لا يلزم استخدامها مع التشغيل العادي.

## 5. شغّل الموقع

1. من قائمة التشغيل بجانب الزر الأخضر اختر **CafeteriaSupplyTask8**.
2. اضغط **Ctrl + F5**.
3. وافق على شهادة HTTPS المحلية إذا طلب Visual Studio ذلك.
4. يفتح المتصفح على `https://localhost:7048`.
5. اضغط **Log in** وادخل البريد وكلمة المرور اللذين اخترتهما في User Secrets.
6. ستظهر قوائم Products وRequests وUsers لحساب SuperAdmin.

إذا احتجت توثيق شهادة التطوير، نفّذ من Terminal:

```powershell
dotnet dev-certs https --trust
```

لتشغيله من Terminal بدل Visual Studio، من مجلد الحل:

```powershell
dotnet run --project .\CafeteriaSupplyTask8
```

## 6. جرّب المتطلبات بالترتيب

1. سجل الدخول بحساب SuperAdmin وتأكد من ظهور المنتجات الستة.
2. أنشئ منتجًا، عدّل سعره وكميته، افتح تفاصيله، واحذف منتجًا تجريبيًا غير مستخدم.
3. سجل خروج، ثم Register باسم كامل وبريد وكلمة مرور. هذا الحساب يأخذ User تلقائيًا ويسجل دخوله.
4. أنشئ طلبًا: Coffee بعدد 2 وMilk بعدد 3. بالأسعار التجريبية 5.50 و1.50، يكون المجموع **15.50** وحالته Pending.
5. عدّل الطلب وهو Pending، ثم أنشئ طلبًا آخر واحذفه لاختبار Delete.
6. افتح متصفحًا آخر/نافذة خاصة وسجل حساب User ثانٍ. يجب ألا يرى طلبات الحساب الأول؛ رابط Details المباشر لطلبه يعيد 404.
7. بحساب SuperAdmin افتح Users → Change role، وغيّر الحساب الثاني إلى Admin. يُلغى تسجيل دخوله القديم ويجب أن يسجل الدخول مجددًا.
8. بحساب Admin: يمكن إدارة المنتجات ورؤية كل الطلبات والموافقة والرفض، ولا يمكن فتح `/Admin` لإدارة المستخدمين.
9. وافق على الطلب الأول. تصبح حالته Approved وتنقص Coffee بمقدار 2 وMilk بمقدار 3. محاولة الموافقة مرة ثانية لا تخصم مجددًا.
10. أنشئ طلبًا مختلفًا وارفضه؛ يصبح Rejected ولا يتغير المخزون.
11. بعد الموافقة أو الرفض لا يستطيع صاحب الطلب تعديله أو حذفه.
12. بحساب SuperAdmin جرّب ترقية حساب آخر إلى SuperAdmin، ثم أعده إلى User. لا يستطيع أي حساب تغيير دوره بنفسه.
13. أنشئ مستخدمًا تجريبيًا واحذفه من Users. صفحة التأكيد توضح أن طلباته وعناصرها تُحذف أيضًا، دون إرجاع مخزون سبق صرفه.
14. جرّب كمية أكبر من المتاح، كمية سالبة، طلبًا دون منتجات، بيانات تسجيل خاطئة، وبريدًا مكررًا. تظهر رسائل تحقق ولا تُحفظ بيانات غير صالحة.

## 7. قرارات العمل التي لم تحددها ورقة التكليف

- Quantity في Product تعني المخزون المتاح للصرف. الطلب يحجز بياناته فقط، والخصم يحصل عند الموافقة داخل transaction.
- كل حساب مسجل يستطيع إنشاء طلب؛ تعديل/حذف الطلب لصاحبه فقط أثناء Pending، حتى لو كان صاحب الطلب Admin.
- Admin وSuperAdmin يستطيعان مراجعة كل الطلبات، بما فيها طلباتهما؛ التكليف لم يفرض فصل مقدم الطلب عن الموافق.
- RequestItem.Price سعر الوحدة وقت إنشاء/تعديل الطلب؛ تغيير سعر المنتج لاحقًا لا يغيّر الطلبات المحفوظة. تعديل الطلب يجدد أسعار جميع عناصره.
- المنتج المشار إليه في طلب لا يُحذف حفاظًا على العلاقة؛ يمكن جعل كميته صفرًا. المنتجات غير المستخدمة قابلة للحذف.
- حذف مستخدم يحذف طلباته وعناصرها بعلاقة cascade، ولا يعكس عمليات صرف مخزون سابقة. شاشة التأكيد تذكر ذلك صراحةً.
- لا يمكن حذف حسابك الحالي أو تغيير دوره بنفسك، ولا حذف/خفض دور آخر SuperAdmin.
- الحساب يمتلك دورًا واحدًا من الأدوار الثلاثة. صلاحيات المدير تشمل SuperAdmin صراحةً في Authorize؛ لا نعتمد وراثة أدوار ضمنية.
- كل التواريخ معروضة بـ UTC والأسعار برقم عشري دون افتراض عملة لم يحددها التكليف.
- العمليات التي تغيّر البيانات POST ومحميّة بـ anti-forgery. تأكيدات Delete عبر GET لا تحذف بيانات.
- RowVersion يحمي من النماذج القديمة، وtransactions تربط تغييرات الطلب بالمخزون.
- التحقق من الحقول في السيرفر مع قيود HTML؛ لا يعتمد أمان النظام على JavaScript.

## 8. خريطة الملفات

| الملف/المجلد | الوظيفة |
|---|---|
| Models/ApplicationUser.cs | توسيع IdentityUser بالاسم الكامل داخل AspNetUsers |
| Models/Product.cs | بيانات المنتج وسعره ومخزونه |
| Models/SupplyRequest.cs | الطلب، صاحبه، حالته وإجماليه |
| Models/RequestItem.cs | منتجات الطلب وكمياتها وأسعارها المحفوظة |
| Data/ApplicationDbContext.cs | العلاقات، دقة الأسعار، قيود قاعدة البيانات |
| Data/DbInitializer.cs | الأدوار، تهيئة أول SuperAdmin، المنتجات التجريبية |
| Controllers/AccountController.cs | Register وLogin وLogout وAccessDenied |
| Controllers/ProductController.cs | Index وCreate وEdit وDetails وDelete |
| Controllers/SupplyRequestController.cs | إدارة الطلبات وApprove وReject وفحص الملكية |
| Controllers/AdminController.cs | إدارة المستخدمين والأدوار، SuperAdmin فقط |
| Services/RequestRules.cs | حساب الأسعار وفحص الكميات وقرار الموافقة/الرفض |
| Services/SupplyRequestService.cs | حفظ الطلبات والـ transactions والتزامن |
| ViewModels | البيانات المسموح استقبالها من النماذج |
| Views | واجهات Razor لكل العمليات |
| wwwroot/css/site.css | التصميم المتجاوب؛ لا يحتاج CDN |
| wwwroot/js/request.js | معاينة المجموع أثناء اختيار الكميات |
| Program.cs | تسجيل Identity وMVC وSQL Server وmiddleware |

## 9. الفحوص والمحددات

من مجلد الحل:

```powershell
dotnet run --project .\CafeteriaSupplyTask8.Checks
```

المتوقع: **41 checks passed**. تشمل الأسعار، حفظ السعر التاريخي، الكميات غير الصالحة، المخزون عند الموافقة والرفض، تكرار الموافقة، تعريف الصلاحيات، وربط جداول Identity.

تم بناء الحل وواجهات Razor دون أخطاء أو تحذيرات، وتوليد SQL والتحقق من تطابق النموذج مع migrations. لم يمكن إجراء اختبار اتصال SQL أو جلسات HTTP فعلية في بيئة الإعداد بسبب قيود LocalDB/Windows authentication؛ لذلك الفحوص الآلية ليست بديلًا عن خطوات التجربة في القسم 6. تفاصيل ذلك في VALIDATION.md.

## 10. حلول المشاكل الشائعة

- **First run: set Bootstrap...**: أضف User Secrets كما في القسم 3، وتأكد أنك تستخدم ملف تشغيل Development المرفق.
- **LocalDB not found / error 50**: ثبّت LocalDB من Visual Studio Installer، ثم نفذ `sqllocaldb start MSSQLLocalDB` في Terminal.
- **Server not found / login failed**: تحقق من اسم السيرفر وصلاحيات حساب Windows. إذا تستخدم SQL Server كاملًا، بدّل Server في DefaultConnection إلى اسم نسختك.
- **NU1202 / .NET not supported**: تأكد من Visual Studio 2026 و.NET 10 SDK والحزم المطابقة.
- **Port already in use**: أغلق نسخة تشغيل سابقة أو غيّر المنافذ في Properties/launchSettings.json.
- **صفحة Login القديمة في /Identity/Account/Login لا تعمل**: المسار الجديد المطلوب في التكليف هو `/Account/Login`؛ الصفحات تستخدم AccountController مخصصًا.
- **لا تظهر Users**: الحساب يحتاج SuperAdmin. بعد تغيير دور مستخدم، يسجل دخوله من جديد.
- **This record changed**: أعد فتح صفحة Edit/Details للحصول على النسخة الحالية ثم كرر العملية.
- **قاعدة البيانات لها جداول دون سجل migrations**: لا تحذفها. استخدم اسم قاعدة جديدًا للتجربة أو راجع سجل migrations؛ المشروع مصمم لقالب Identity الأصلي أو قاعدة جديدة.

مراجع: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles وhttps://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
