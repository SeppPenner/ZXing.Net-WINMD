﻿/*
 * Copyright 2012 ZXing.Net authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media.Imaging;
using ZXing.Common;
using ZXing.Multi;
using ZXing.Multi.QrCode;

namespace ZXing
{
   /// <summary>
   /// A smart class to decode the barcode inside a bitmap object
   /// </summary>
   public sealed class BarcodeReader : IBarcodeReader // , IMultipleBarcodeReader
   {
      private static readonly Func<LuminanceSource, Binarizer> defaultCreateBinarizer =
         (luminanceSource) => new HybridBinarizer(luminanceSource);

      private static readonly Func<byte[], int, int, BitmapFormat, LuminanceSource> defaultCreateRGBLuminanceSource =
         (rawBytes, width, height, format) => new RGBLuminanceSource(rawBytes, width, height, format);
      private static readonly Func<WriteableBitmap, LuminanceSource> defaultCreateLuminanceSource =
         (bitmap) => new BitmapLuminanceSource(bitmap);

      private Reader reader;
      private readonly IDictionary<DecodeHintType, object> hints;
      private readonly Func<byte[], int, int, BitmapFormat, LuminanceSource> createRGBLuminanceSource;
      private readonly Func<WriteableBitmap, LuminanceSource> createLuminanceSource;
      private readonly Func<LuminanceSource, Binarizer> createBinarizer;
      private bool usePreviousState;

      /// <summary>
      /// Gets the reader which should be used to find and decode the barcode.
      /// </summary>
      /// <value>
      /// The reader.
      /// </value>
      internal Reader Reader
      {
         get
         {
            return reader ?? (reader = new MultiFormatReader());
         }
      }

      /// <summary>
      /// event is executed if a result was found via decode
      /// </summary>
      //public event Action<Result> ResultFound;

      /// <summary>
      /// Gets or sets a flag which cause a deeper look into the bitmap
      /// </summary>
      /// <value>
      ///   <c>true</c> if [try harder]; otherwise, <c>false</c>.
      /// </value>
      public bool TryHarder
      {
         get
         {
            if (hints.ContainsKey(DecodeHintType.TRY_HARDER))
               return (bool)hints[DecodeHintType.TRY_HARDER];
            return false;
         }
         set
         {
            if (value)
            {
               hints[DecodeHintType.TRY_HARDER] = true;
               usePreviousState = false;
            }
            else
            {
               if (hints.ContainsKey(DecodeHintType.TRY_HARDER))
               {
                  hints.Remove(DecodeHintType.TRY_HARDER);
                  usePreviousState = false;
               }
            }
         }
      }

      /// <summary>
      /// Image is a pure monochrome image of a barcode.
      /// </summary>
      /// <value>
      ///   <c>true</c> if monochrome image of a barcode; otherwise, <c>false</c>.
      /// </value>
      public bool PureBarcode
      {
         get
         {
            if (hints.ContainsKey(DecodeHintType.PURE_BARCODE))
               return (bool)hints[DecodeHintType.PURE_BARCODE];
            return false;
         }
         set
         {
            if (value)
            {
               hints[DecodeHintType.PURE_BARCODE] = true;
               usePreviousState = false;
            }
            else
            {
               if (hints.ContainsKey(DecodeHintType.PURE_BARCODE))
               {
                  hints.Remove(DecodeHintType.PURE_BARCODE);
                  usePreviousState = false;
               }
            }
         }
      }

      /// <summary>
      /// Specifies what character encoding to use when decoding, where applicable (type String)
      /// </summary>
      /// <value>
      /// The character set.
      /// </value>
      public string CharacterSet
      {
         get
         {
            if (hints.ContainsKey(DecodeHintType.CHARACTER_SET))
               return (string)hints[DecodeHintType.CHARACTER_SET];
            return null;
         }
         set
         {
            if (value != null)
            {
               hints[DecodeHintType.CHARACTER_SET] = value;
               usePreviousState = false;
            }
            else
            {
               if (hints.ContainsKey(DecodeHintType.CHARACTER_SET))
               {
                  hints.Remove(DecodeHintType.CHARACTER_SET);
                  usePreviousState = false;
               }
            }
         }
      }

      /// <summary>
      /// Image is known to be of one of a few possible formats.
      /// Maps to a {@link java.util.List} of {@link BarcodeFormat}s.
      /// </summary>
      /// <value>
      /// The possible formats.
      /// </value>
      public IList<BarcodeFormat> PossibleFormats
      {
         get
         {
            if (hints.ContainsKey(DecodeHintType.POSSIBLE_FORMATS))
               return (IList<BarcodeFormat>)hints[DecodeHintType.POSSIBLE_FORMATS];
            return null;
         }
         set
         {
            if (value != null)
            {
               hints[DecodeHintType.POSSIBLE_FORMATS] = value;
               usePreviousState = false;
            }
            else
            {
               if (hints.ContainsKey(DecodeHintType.POSSIBLE_FORMATS))
               {
                  hints.Remove(DecodeHintType.POSSIBLE_FORMATS);
                  usePreviousState = false;
               }
            }
         }
      }

      /// <summary>
      /// Gets or sets a value indicating whether the image should be automatically rotated.
      /// Rotation is supported for 90, 180 and 270 degrees
      /// </summary>
      /// <value>
      ///   <c>true</c> if image should be rotated; otherwise, <c>false</c>.
      /// </value>
      public bool AutoRotate { get; set; }

      /// <summary>
      /// Optional: Gets or sets the function to create a luminance source object for a bitmap.
      /// If null a platform specific default LuminanceSource is used
      /// </summary>
      /// <value>
      /// The function to create a luminance source object.
      /// </value>
      internal Func<WriteableBitmap, LuminanceSource> CreateLuminanceSource
      {
         get
         {
            return createLuminanceSource ?? defaultCreateLuminanceSource;
         }
      }

      /// <summary>
      /// Optional: Gets or sets the function to create a binarizer object for a luminance source.
      /// If null then HybridBinarizer is used
      /// </summary>
      /// <value>
      /// The function to create a binarizer object.
      /// </value>
      internal Func<LuminanceSource, Binarizer> CreateBinarizer
      {
         get
         {
            return createBinarizer ?? defaultCreateBinarizer;
         }
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeReader"/> class.
      /// </summary>
      public BarcodeReader()
         : this(new MultiFormatReader(), null, defaultCreateBinarizer)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeReader"/> class.
      /// </summary>
      /// <param name="reader">Sets the reader which should be used to find and decode the barcode.
      /// If null then MultiFormatReader is used</param>
      /// <param name="createLuminanceSource">Sets the function to create a luminance source object for a bitmap.
      /// If null, an exception is thrown when Decode is called</param>
      /// <param name="createBinarizer">Sets the function to create a binarizer object for a luminance source.
      /// If null then HybridBinarizer is used</param>
      internal BarcodeReader(Reader reader,
         Func<WriteableBitmap, LuminanceSource> createLuminanceSource,
         Func<LuminanceSource, Binarizer> createBinarizer
         )
         : this(reader, createLuminanceSource, createBinarizer, null)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeReader"/> class.
      /// </summary>
      /// <param name="reader">Sets the reader which should be used to find and decode the barcode.
      /// If null then MultiFormatReader is used</param>
      /// <param name="createLuminanceSource">Sets the function to create a luminance source object for a bitmap.
      /// If null, an exception is thrown when Decode is called</param>
      /// <param name="createBinarizer">Sets the function to create a binarizer object for a luminance source.
      /// If null then HybridBinarizer is used</param>
      /// <param name="createRGBLuminanceSource">Sets the function to create a luminance source object for a rgb array.
      /// If null the RGBLuminanceSource is used. The handler is only called when Decode with a byte[] array is called.</param>
      internal BarcodeReader(Reader reader,
         Func<WriteableBitmap, LuminanceSource> createLuminanceSource,
         Func<LuminanceSource, Binarizer> createBinarizer,
         Func<byte[], int, int, BitmapFormat, LuminanceSource> createRGBLuminanceSource
         )
      {
         this.reader = reader ?? new MultiFormatReader();
         this.createLuminanceSource = createLuminanceSource;
         this.createBinarizer = createBinarizer ?? defaultCreateBinarizer;
         this.createRGBLuminanceSource = createRGBLuminanceSource ?? defaultCreateRGBLuminanceSource;
         hints = new Dictionary<DecodeHintType, object>();
         usePreviousState = false;
      }

      /// <summary>
      /// Decodes the specified barcode bitmap.
      /// </summary>
      /// <param name="barcodeBitmap">The barcode bitmap.</param>
      /// <returns>the result data or null</returns>
      public Result Decode(WriteableBitmap barcodeBitmap)
      {
         if (CreateLuminanceSource == null)
         {
            throw new InvalidOperationException("You have to declare a luminance source delegate.");
         }

         if (barcodeBitmap == null)
            throw new ArgumentNullException("barcodeBitmap");

         var luminanceSource = CreateLuminanceSource(barcodeBitmap);

         return Decode(luminanceSource);
      }

      private Result Decode(LuminanceSource luminanceSource)
      {
         var result = default(Result);
         var binarizer = CreateBinarizer(luminanceSource);
         var binaryBitmap = new BinaryBitmap(binarizer);
         var multiformatReader = Reader as MultiFormatReader;
         var rotationCount = 0;
         var rotationMaxCount = AutoRotate ? 4 : 1;

         for (; rotationCount < rotationMaxCount; rotationCount++)
         {
            if (usePreviousState && multiformatReader != null)
            {
               result = multiformatReader.decodeWithState(binaryBitmap);
            }
            else
            {
               result = Reader.decode(binaryBitmap, hints);
               usePreviousState = true;
            }

            if (result != null ||
                !luminanceSource.RotateSupported ||
                !AutoRotate)
               break;

            binaryBitmap = new BinaryBitmap(CreateBinarizer(luminanceSource.rotateCounterClockwise()));
         }

         if (result != null)
         {
            if (result.ResultMetadata == null)
            {
               result.putMetadata(ResultMetadataType.ORIENTATION, rotationCount * 90);
            }
            else if (!result.ResultMetadata.ContainsKey(ResultMetadataType.ORIENTATION))
            {
               result.ResultMetadata[ResultMetadataType.ORIENTATION] = rotationCount * 90;
            }
            else
            {
               // perhaps the core decoder rotates the image already (can happen if TryHarder is specified)
               result.ResultMetadata[ResultMetadataType.ORIENTATION] = ((int)(result.ResultMetadata[ResultMetadataType.ORIENTATION]) + rotationCount * 90) % 360;
            }

//            OnResultFound(result);
         }

         return result;
      }

      /// <summary>
      /// Decodes the specified barcode bitmap.
      /// </summary>
      /// <param name="barcodeBitmap">The barcode bitmap.</param>
      /// <returns>the result data or null</returns>
      public Result[] DecodeMultiple(WriteableBitmap barcodeBitmap)
      {
         if (CreateLuminanceSource == null)
         {
            throw new InvalidOperationException("You have to declare a luminance source delegate.");
         }
         if (barcodeBitmap == null)
            throw new ArgumentNullException("barcodeBitmap");

         var luminanceSource = CreateLuminanceSource(barcodeBitmap);

         return DecodeMultiple(luminanceSource);
      }

      private Result[] DecodeMultiple(LuminanceSource luminanceSource)
      {
         var results = default(Result[]);
         var binarizer = CreateBinarizer(luminanceSource);
         var binaryBitmap = new BinaryBitmap(binarizer);
         var rotationCount = 0;
         var rotationMaxCount = AutoRotate ? 4 : 1;
         MultipleBarcodeReader multiReader = null;

         var formats = PossibleFormats;
         if (formats != null &&
             formats.Count == 1 &&
             formats.Contains(BarcodeFormat.QR_CODE))
         {
            multiReader = new QRCodeMultiReader();
         }
         else
         {
            multiReader = new GenericMultipleBarcodeReader(Reader);
         }

         for (; rotationCount < rotationMaxCount; rotationCount++)
         {
            results = multiReader.decodeMultiple(binaryBitmap, hints);

            if (results != null ||
                !luminanceSource.RotateSupported ||
                !AutoRotate)
               break;

            binaryBitmap = new BinaryBitmap(CreateBinarizer(luminanceSource.rotateCounterClockwise()));
         }

         if (results != null)
         {
            foreach (var result in results)
            {
               if (result.ResultMetadata == null)
               {
                  result.putMetadata(ResultMetadataType.ORIENTATION, rotationCount * 90);
               }
               else if (!result.ResultMetadata.ContainsKey(ResultMetadataType.ORIENTATION))
               {
                  result.ResultMetadata[ResultMetadataType.ORIENTATION] = rotationCount * 90;
               }
               else
               {
                  // perhaps the core decoder rotates the image already (can happen if TryHarder is specified)
                  result.ResultMetadata[ResultMetadataType.ORIENTATION] =
                     ((int)(result.ResultMetadata[ResultMetadataType.ORIENTATION]) + rotationCount * 90) % 360;
               }
            }

//            OnResultsFound(results);
         }

         return results;
      }

      //      protected void OnResultsFound(IEnumerable<Result> results)
//      {
//         if (ResultFound != null)
//         {
//            foreach (var result in results)
//            {
//               ResultFound(result);
//            }
//         }
//      }

//      protected void OnResultFound(Result result)
//      {
//         if (ResultFound != null)
//         {
//            ResultFound(result);
//         }
//      }

      /// <summary>
      /// Decodes the specified barcode bitmap.
      /// </summary>
      /// <param name="rawRGB">The image as byte[] array.</param>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      /// <param name="format">The format.</param>
      /// <returns>
      /// the result data or null
      /// </returns>
      public Result Decode([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray]byte[] rawRGB, int width, int height, BitmapFormat format)
      {
         if (rawRGB == null)
            throw new ArgumentNullException("rawRGB");
         
         var luminanceSource = createRGBLuminanceSource(rawRGB, width, height, format);

         return Decode(luminanceSource);
      }

      /// <summary>
      /// Decodes the specified barcode bitmap.
      /// </summary>
      /// <param name="rawRGB">The image as byte[] array.</param>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      /// <param name="format">The format.</param>
      /// <returns>
      /// the result data or null
      /// </returns>
      public Result[] DecodeMultiple([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray]byte[] rawRGB, int width, int height, BitmapFormat format)
      {
         if (rawRGB == null)
            throw new ArgumentNullException("rawRGB");
         
         var luminanceSource = createRGBLuminanceSource(rawRGB, width, height, format);

         return DecodeMultiple(luminanceSource);
      }
   }
}
