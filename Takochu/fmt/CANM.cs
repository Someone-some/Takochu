using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Takochu.io;

namespace Takochu.fmt
{
    public class CANM
    {
        public CANM(FileBase file)
        {
            mFile = file;

            if (file.ReadString(0x8) != "ANDOKAN")
                throw new Exception("CANM::CANM -- Invalid CANM file!");

            mHeader = new Header(mFile);

            for (int i = 0; i < 8; i++)
                mKeys.Add(new Keyset(mFile, i));

            foreach (Keyset ks in mKeys)
                ks.GetKeyFrames(mFile);  
        }

        public void Save()
        {
            mFile.WriteString("ANDOKAN");

            mFile.Write(mHeader.unk0);
            mFile.Write(mHeader.unk1);
            mFile.Write(mHeader.unk2);
            mFile.Write(mHeader.unk3);
            mFile.Write(mHeader.unk4);
            mFile.Write(mHeader.mFloatStart);

            for (int i = 0; i < mKeys.Count; i++)
            {
                
            }
        }

        private FileBase mFile;
        private Header mHeader;
        private List<Keyset> mKeys = new List<Keyset>();
    }

    public class Header
    {
        public Header(FileBase canmFile)
        {
            unk0 = BitConverter.ToInt32(canmFile.ReadBytes(4), 0);
            unk1 = BitConverter.ToInt32(canmFile.ReadBytes(4), 0);
            unk2 = BitConverter.ToInt32(canmFile.ReadBytes(4), 0);
            unk3 = BitConverter.ToInt32(canmFile.ReadBytes(4), 0);
            unk4 = BitConverter.ToInt32(canmFile.ReadBytes(4), 0);
            mFloatStart = BitConverter.ToInt32(canmFile.ReadBytes(4), 0);
        }

        public int unk0;
        public int unk1;
        public int unk2;
        public int unk3;
        public int unk4;
        public int mFloatStart; // 0x00000060
    }

    public class Keyset
    {
        public Keyset(FileBase canmFile, int value)
        {
            mWtfIsThisFor = value;
            mNumKeyframes = BitConverter.ToInt32(canmFile.ReadBytes(4), 0);
            mDataIndex = BitConverter.ToInt32(canmFile.ReadBytes(4), 0);
            mPadSize = BitConverter.ToInt32(canmFile.ReadBytes(4), 0);
        }

        public Keyset GetKeyFrames(FileBase canmFile)
        {
            for (int i = 0; i < mNumKeyframes; i++)
            {
                Keyframes.Add(new Keyframe(canmFile, mNumKeyframes == 1));
            }

            return this;
        }

        public List<Keyframe> Keyframes = new List<Keyframe>();

        public int mWtfIsThisFor;
        public int mNumKeyframes;
        public int mDataIndex;
        public int mPadSize;
    }

    public class Keyframe
    {
        public Keyframe(FileBase canmFile, bool isSingle)
        {
            if (isSingle)
                mValue = BitConverter.ToSingle(canmFile.ReadBytes(4), 0);
            else
            {
                mFrames = BitConverter.ToSingle(canmFile.ReadBytes(4), 0);
                mValue = BitConverter.ToSingle(canmFile.ReadBytes(4), 0);
                mVelocity = BitConverter.ToSingle(canmFile.ReadBytes(4), 0);
            }
        }

        public Keyframe(float frames, float val, float velocity)
        {
            mFrames = 0;
            mValue = 0;
            mVelocity = 0;
        }

        public float mFrames;
        public float mValue;
        public float mVelocity;
    }
}
