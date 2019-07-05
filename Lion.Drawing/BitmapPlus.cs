using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.IO;
using System.Text;

namespace Lion.Drawing
{
    public static class BitmapPlus
    {
        #region Crop
        public static byte[] Crop(byte[] _source, int _source_x, int _source_y, int _source_w, int _source_h, int _target_w, int _target_h, ImageCodecInfo _codecInfo, EncoderParameters _paraments)
        {
            return BitmapPlus.Crop(new MemoryStream(_source), _source_x, _source_y, _source_w, _source_h, _target_w, _target_h, _codecInfo, _paraments);
        }
        public static byte[] Crop(Stream _stream, int _source_x, int _source_y, int _source_w, int _source_h, int _target_w, int _target_h, ImageCodecInfo _codecInfo, EncoderParameters _paraments)
        {
            Bitmap _source_bitmap = (Bitmap)Image.FromStream(_stream);
            Bitmap _target_bitmap = new Bitmap(_target_w, _target_h);

            Graphics _graphics = Graphics.FromImage(_target_bitmap);
            _graphics.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, _target_bitmap.Width, _target_bitmap.Height));
            _graphics.DrawImage(_source_bitmap, new Rectangle(0, 0, _target_w, _target_h), new Rectangle(_source_x, _source_y, _source_w, _source_h), GraphicsUnit.Pixel);
            _graphics.Dispose();

            _stream.Close();

            MemoryStream _streamOutput = new MemoryStream();
            _target_bitmap.Save(_streamOutput, _codecInfo, _paraments);
            byte[] _target = _streamOutput.ToArray();
            _streamOutput.Close();

            return _target;
        }
        #endregion

        #region Resize
        public static byte[] Resize(byte[] _source, int _target_width, int _target_height, ImageCodecInfo _codecInfo, EncoderParameters _paraments)
        {
            return BitmapPlus.Resize(new MemoryStream(_source), _target_width, _target_height, _codecInfo, _paraments);
        }
        public static byte[] Resize(Stream _stream, int _target_width, int _target_height, ImageCodecInfo _codecInfo, EncoderParameters _paraments)
        {
            Bitmap _source_bitmap = (Bitmap)Image.FromStream(_stream);

            int _target_x = 0;
            int _target_y = 0;
            int _source_x = 0;
            int _source_y = 0;
            int _source_width = 0;
            int _source_height = 0;

            Bitmap _target_bitmap = new Bitmap(_target_width, _target_height);

            double _target_rate = (double)_target_width / (double)_target_height;
            double _source_rate = (double)_source_bitmap.Width / (double)_source_bitmap.Height;

            if (_source_bitmap.Width < _target_width || _source_bitmap.Height < _target_height)
            {
                _source_width = _source_bitmap.Width;
                _source_height = _source_bitmap.Height;
                _target_x = (_target_width - _source_width) / 2;
                _target_y = (_target_height - _source_height) / 2;
                _target_width = _source_width;
                _target_height = _source_height;
            }
            else
            {
                _source_width = _target_rate >= _source_rate ? _source_bitmap.Width : (int)(_source_bitmap.Height * _target_rate);
                _source_height = _target_rate >= _source_rate ? (int)(_source_bitmap.Width / _target_rate) : _source_bitmap.Height;
                _source_x = (_source_bitmap.Width - _source_width) / 2;
                _source_y = (_source_bitmap.Height - _source_height) / 2;
            }

            Graphics _graphics = Graphics.FromImage(_target_bitmap);
            _graphics.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, _target_bitmap.Width, _target_bitmap.Height));
            _graphics.DrawImage(_source_bitmap, new Rectangle(_target_x, _target_y, _target_width, _target_height), new Rectangle(_source_x, _source_y, _source_width, _source_height), GraphicsUnit.Pixel);
            _graphics.Dispose();

            _stream.Close();

            MemoryStream _streamOutput = new MemoryStream();
            _target_bitmap.Save(_streamOutput, _codecInfo, _paraments);
            byte[] _target = _streamOutput.ToArray();
            _streamOutput.Close();

            return _target;
        }
        #endregion

        #region ImageCodecJpeg
        public static ImageCodecInfo ImageCodecJpeg
        {
            get
            {
                ImageCodecInfo[] _encoders;
                _encoders = ImageCodecInfo.GetImageEncoders();
                for (int _index = 0; _index < _encoders.Length; _index++)
                {
                    if (_encoders[_index].MimeType == "image/jpeg")
                        return _encoders[_index];
                }
                return null;
            }
        }
        #endregion

        #region ImageCodecPng
        public static ImageCodecInfo ImageCodecPng
        {
            get
            {
                ImageCodecInfo[] _encoders;
                _encoders = ImageCodecInfo.GetImageEncoders();
                for (int _index = 0; _index < _encoders.Length; _index++)
                {
                    if (_encoders[_index].MimeType == "image/png")
                        return _encoders[_index];
                }
                return null;
            }
        }
        #endregion

        #region EncoderParametersDefault
        public static EncoderParameters EncoderParametersDefault
        {
            get
            {
                EncoderParameters _encoderParaments = new EncoderParameters(2);
                _encoderParaments.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 24L);
                _encoderParaments.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);
                return _encoderParaments;
            }
        }
        #endregion

        #region GenerateCheckCode
        public static byte[] GenerateCheckCode(string _code)
        {
            Bitmap _image = new System.Drawing.Bitmap((int)Math.Ceiling((_code.Length * 32.5)), 30);
            Graphics _graphics = Graphics.FromImage(_image);
            byte[] _binary = new byte[0];

            try
            {
                Random _random = new Random();
                _graphics.Clear(Color.White);

                for (int _index = 0; _index < 20; _index++)
                {
                    int _x1 = _random.Next(_image.Width);
                    int _x2 = _random.Next(_image.Width);
                    int _y1 = _random.Next(_image.Height);
                    int _y2 = _random.Next(_image.Height);
                    _graphics.DrawLine(new Pen(Color.Silver), _x1, _y1, _x2, _y2);
                }

                Color[] _colors = { Color.Black, Color.Red, Color.DarkBlue, Color.Green, Color.Orange, Color.Brown, Color.DarkCyan, Color.Purple };
                string[] _fonts = { "Verdana", "Microsoft Sans Serif", "Comic Sans MS", "Arial", "宋体" };

                for (int _index = 0; _index <= _code.Length - 1; _index++)
                {
                    int _cindex = _random.Next(7);
                    int _findex = _random.Next(5);

                    Font _drawFont = new Font(_fonts[_findex], 16, (System.Drawing.FontStyle.Bold));
                    SolidBrush _drawBrush = new SolidBrush(_colors[_cindex]);

                    float _x = 5.0F;
                    float _y = 0.0F;
                    float _w = 20.0F;
                    float _h = 25.0F;
                    int _sjx = _random.Next(10);
                    int _sjy = _random.Next(_image.Height - (int)_h);

                    RectangleF _drawRect = new RectangleF(_x + _sjx + (_index * 25), _y + _sjy, _w, _h);
                    StringFormat _drawFormat = new StringFormat();
                    _drawFormat.Alignment = StringAlignment.Center;
                    _graphics.DrawString(_code[_index].ToString(), _drawFont, _drawBrush, _drawRect, _drawFormat);
                }

                for (int _index = 0; _index < 100; _index++)
                {
                    int _x = _random.Next(_image.Width);
                    int _y = _random.Next(_image.Height);
                    _image.SetPixel(_x, _y, Color.FromArgb(_random.Next()));
                }
                _graphics.DrawRectangle(new Pen(Color.Silver), 0, 0, _image.Width - 1, _image.Height - 1);

                MemoryStream _stream = new System.IO.MemoryStream();
                _image.Save(_stream, System.Drawing.Imaging.ImageFormat.Gif);
                _binary = _stream.ToArray();
                _stream.Close();
            }
            finally
            {
                _graphics.Dispose();
                _image.Dispose();
            }
            return _binary;
        }
        #endregion

        #region GeneratePixelImage
        public static Image GeneratePixelImage(int _imageSize = 240, int _tileSize = 20, int _margin = 20, bool _mirrorX = true, bool _mirrorY = false, bool _border = true, string _borderColor = "#000000", Image _backGround = null)
        {

            var _ranks = (_imageSize - _margin * 2) / _tileSize;
            var _image = new System.Drawing.Bitmap(_imageSize, _imageSize);
            using (Graphics _g = Graphics.FromImage(_image))
            {
                if (_backGround != null)
                    _g.DrawImage(_backGround, 0, 0, _imageSize, _imageSize);                
                else
                    _g.FillRectangle(new SolidBrush(Color.White),0,0,_imageSize,_imageSize);
                if (_border)
                {
                    Color _bdColor = ColorTranslator.FromHtml(_borderColor);
                    var _pen = new Pen(_bdColor, 1);
                    for (var x = 0; x <= _tileSize * _ranks; x += _tileSize)
                    {
                        _g.DrawLine(_pen, x + _margin, _margin, x + _margin, _tileSize * _ranks + _margin);
                    }
                    for (var y = 0; y <= _tileSize * _ranks; y += _tileSize)
                    {
                        _g.DrawLine(_pen, _margin, y + _margin, _tileSize * _ranks + _margin, y + _margin);
                    }
                }
                List<Tuple<int, int>> _boxes = new List<Tuple<int, int>>();
                var _randomRegionX = _mirrorX ? _ranks / 2 + 1 : _ranks;
                var _randomRegionY = _mirrorY ? _ranks / 2 + 1 : _ranks;
                var _tileCountLimit = _mirrorX && _mirrorY ? (_ranks * _ranks) / 4 : !_mirrorX && !_mirrorY ? _ranks * _ranks : (_ranks * _ranks) / 2;
                var _count = new Random(RandomPlus.RandomSeed).Next(5, _tileCountLimit);
                while (_boxes.Count() < _count)
                {
                    int _x = new Random(RandomPlus.RandomSeed).Next(0, _randomRegionX);
                    int _y = new Random(RandomPlus.RandomSeed).Next(0, _randomRegionY);
                    if (_boxes.Count(t => t.Item1 == _x && t.Item2 == _y) <= 0)
                        _boxes.Add(new Tuple<int, int>(_x, _y));
                }
                var _colorG = new Random(RandomPlus.RandomSeed).Next(0, 200);
                var _colorR = new Random(RandomPlus.RandomSeed).Next(0, 200);
                var _colorB = new Random(RandomPlus.RandomSeed).Next(0, 200);
                Color _color = Color.FromArgb(255, _colorR, _colorG, _colorB);
                Brush _brush = new Pen(_color).Brush;

                for (var i = 0; i < _boxes.Count; i++)
                {
                    var _boxDraw = _boxes[i];
                    _g.FillRectangle(_brush, _boxDraw.Item1 * _tileSize + _margin, _boxDraw.Item2 * _tileSize + _margin, _tileSize, _tileSize);
                    if (_mirrorX)
                        _g.FillRectangle(_brush, (int)((_ranks - _boxDraw.Item1 - 1) * _tileSize + _margin), _boxDraw.Item2 * _tileSize + _margin, _tileSize, _tileSize);
                    if (_mirrorY)
                        _g.FillRectangle(_brush, _boxDraw.Item1 * _tileSize + _margin, (int)((_ranks - _boxDraw.Item2 - 1) * _tileSize + _margin), _tileSize, _tileSize);
                    if (_mirrorX && _mirrorY)
                        _g.FillRectangle(_brush, (int)((_ranks - _boxDraw.Item1 - 1) * _tileSize + _margin), (int)((_ranks - _boxDraw.Item2 - 1) * _tileSize + _margin), _tileSize, _tileSize);
                }
                _g.Save();
            }
            return _image;
        }
        #endregion
    }
}
