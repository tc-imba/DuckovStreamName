# 直播间观众Mod

可以将怪物名字替换成b站直播间观众的Mod，后续看情况可能会增加其他直播网站（比如抖音，斗鱼，虎牙）的支持

默认情况下，Mod会根据直播间当前贡献度排名作为优先级来替换怪物的名字，贡献度越高，名字被使用的概率越大

由于使用了带保底伪随机算法，可以保证不会有人一直无法被抽到，另外由于b站API限制，暂时只支持显示直播间前100贡献度的用户

作者：天才IMBA（双酿）

## 前置Mod

请安装并激活以下前置Mod，请把它们放在本Mod之前并重启游戏：

+ HarmonyLib: https://steamcommunity.com/sharedfiles/filedetails/?id=3589088839
+ ModConfig: https://steamcommunity.com/sharedfiles/filedetails/?id=3590674339

## Mod兼容性

本Mod和其他显示或修改NPC名称的Mod可能发生冲突，建议禁用

目前为以下Mod提供了额外的适配，如果使用了他们，请把它们放在本Mod之前并重启游戏：

+ KillFeed(击杀记录): https://steamcommunity.com/sharedfiles/filedetails/?id=3588412062
+ 战地风格动态击杀提示: https://steamcommunity.com/sharedfiles/filedetails/?id=3591589560

## 配置

所有配置都可以通过ModConfig更改，必须先进入游戏（地堡），然后在ESC的设置里可以看到配置

### 直播间配置

目前只支持b站，需要输入正确的房间号和主播的uid，由于b站的这个api不需要登录，所以可以使用任何主播的直播间，但是主播的uid和房间号必须正确匹配，这里简单演示一下获取uid和房间号的方法

以主播苏烟Suyn为例：

主播的b站首页是https://space.bilibili.com/33004908

主播的直播间是https://live.bilibili.com/1851740

所以我们可以得到uid：33004908，房间号：1851740

之后在配置界面输入这两个值即可，注意不要输反了

### 舰长贡献度配置

本Mod还可以给直播间舰长设置更高的贡献值权重，分为两个参数：舰长基础贡献值和舰长贡献值额外倍率，舰长的贡献值会按照 舰长基础贡献值 + (1 + 舰长贡献值额外倍率) * 用户当前贡献值来计算

由于本直播间暂时没有提督和总督，所以还没加入单独的提督和总督倍率设置

## 鸣谢

借鉴了一些以下Mod的代码：

+ NPC 随机 Steam 好友名称 https://steamcommunity.com/sharedfiles/filedetails/?id=3592504333

## 开源和支持

本项目在GitHub开源：https://github.com/tc-imba/DuckovStreamName

如果有任何BUG或者建议，可以在创意工坊提出，或者在GitHub提交Issue

