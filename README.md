# Unity-Style-Control-System
本项目基于Unity URP开发，实现了一个可实时调节的风格化渲染系统，支持从写实到卡通风格的连续过渡。  同时接入Stable Diffusion（秋叶整合包），通过 AI 生成图像并提取其视觉特征（颜色 / 亮度 / 饱和度），映射为 Unity 中的材质与渲染参数，实现“AI风格 → 实时渲染”的转换流程。
## ✨ 功能特点

- 🎨 **实时风格控制**
    
    - 风格化程度（Stylization）
        
    - 明暗控制（Darkness）
        
    - 色调控制（Color Tint）
        
    - Ramp 阴影控制（未完成）
        
- 💡 **支持 URP 光照系统**
    
    - 主光源（Directional Light）
        
    - 额外光源（Point / Spot Light）
        
- 🤖 **AI风格驱动**
    
    - 输入 Prompt 生成图像
        
    - 自动提取图像特征：
        
        - 平均颜色 → 色调（Tint）
            
        - 亮度 → 明暗（Darkness）
            
        - 饱和度 → 风格化程度（Stylization）
            
- 🧱 **编辑器工具（Editor Tool）**
    
    - 自动扫描场景材质
        
    - 批量应用风格参数
        
    - 实时预览效果
        

---


## 🧠 技术要点

- 自定义 URP Shader（写实 + 卡通混合光照模型）
    
- Ramp 贴图控制阴影过渡(未完成）
    
- 支持 URP Additional Lights（多光源）
    
- 图像分析与参数映射：
    
    - 像素采样
        
    - 颜色统计
        
    - 参数归一化映射
        
- Unity 编辑器扩展开发（EditorWindow 工具）
    

---

## 🛠️ 使用方法

1. 使用 Unity 打开项目（需启用 URP）
    
2. 打开工具：
    
    ```
    Tools → Style Tool
    ```
    
3. 点击：
    
    - Scan Scene Materials（扫描材质）
        
4. 调整参数，实时查看效果
    

### （可选）AI生成

需自行部署 **Stable Diffusion(秋葉整合包)** 并启用api

1. 输入 Prompt
    
2. 点击 Generate
    
3. 自动提取风格并应用
    

---

## ⚙️ 环境要求

- Unity 2022 及以上（URP）
    
- Stable Diffusion WebUI（用于AI功能）
    
- .NET 4.x
    

---

## 📂 项目结构

```id="r7g2aa"
Assets/
├── Shader/        // 自定义Shader
├── Editor/        // 工具代码
├── Scripts/       // 逻辑代码
├── Screenshots/   // 展示图片
```

---

## 🎯 项目目的

本项目旨在探索：

> 如何将 AI 生成的视觉风格，转化为实时渲染中的可控参数

并验证以下方向：

- AI辅助风格设计
    
- 技术美术（TA）工具开发
    
- 风格化渲染流程搭建
    

---



## 🧩 后续优化方向

- 全局风格控制（Render Feature）
    
- Shader Graph 重构版本
    
- 更精确的AI风格映射
    
- 支持多种Shader兼容
    

---

## 🧾 开源协议

MIT License


---

## 📖 一句话总结

> 一个结合 AI 与实时渲染的风格控制系统，用于探索图像风格到Shader参数的映射关系。
