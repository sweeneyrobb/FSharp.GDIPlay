﻿namespace Sophcon.Builder.Imaging

open System.Drawing
open System.Drawing.Imaging
open System.Runtime.InteropServices

module Tools =
    type ImageByteData = {
        data:byte[];
        stride:int
    }

    let adjustLevel (channelByte:float32, level:float32) =
        channelByte * level

    let setInRGBRange channelByte =
        System.Math.Min(System.Math.Max(channelByte, 255.0f), 0.0f)

    let multiplyChannelByte (sourceChannel:byte) (overlayChannel:byte) =
        (float32(sourceChannel)/255.0f * float32(overlayChannel)/255.0f) * 255.0f |> byte

    let pixelMap2 (sourceImageData:byte[]) (overlayImageData:byte[]) (blendFunction) = 
        Array.map2 blendFunction sourceImageData overlayImageData

    let createSolidColorImage width height color  = 
        let image = new Bitmap(width, height, PixelFormat.Format32bppArgb)
        let graphics = Graphics.FromImage(image)

        graphics.FillRectangle(new SolidBrush(color), 0, 0, width, height)
        image

    let createSolidColorOverlay (image:Bitmap) (color:Color) = 
        createSolidColorImage image.Width image.Height color

    let newImageByteArray (imageData:BitmapData) =
        Array.zeroCreate<byte>(imageData.Stride * imageData.Height)

    let getByteArrayForImage (image:Bitmap) =
        let rect = Rectangle(0, 0, image.Width, image.Height)
        let data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        let buffer = newImageByteArray data

        Marshal.Copy(data.Scan0, buffer, 0, buffer.Length)

        image.UnlockBits data

        { data = buffer; stride = data.Stride }

    let blendImages (baseImage:Bitmap) (overlayImage:Bitmap) =
        let baseData = getByteArrayForImage baseImage
        let overlayData = getByteArrayForImage overlayImage
    
        let resultData = pixelMap2 baseData.data overlayData.data multiplyChannelByte    

        let resultImage = new Bitmap(baseImage.Width, baseImage.Height, PixelFormat.Format32bppArgb)
        let resultImageData = resultImage.LockBits(new Rectangle(0, 0, resultImage.Width, resultImage.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb)

        Marshal.Copy(resultData, 0, resultImageData.Scan0, resultData.Length)

        resultImage.UnlockBits resultImageData

        resultImage
        
    let colorTransform image color =
        createSolidColorOverlay image color
        |> blendImages image