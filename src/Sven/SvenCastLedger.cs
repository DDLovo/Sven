using System;
using System.Collections.Generic;

namespace SvenMod
{
    // The native state channel and R2API message channel may arrive out of order.
    // Each native state authorizes only its own numbered cast and shot count.
    public sealed class SvenCastLedger<T>
    {
        private sealed class Cast { public int kind,count,mask; public float expires; }
        private struct Request { public uint cast; public int kind,index; public float expires; public T payload; }
        private readonly Dictionary<uint,Cast> casts=new Dictionary<uint,Cast>();
        private readonly List<Request> pending=new List<Request>();
        public int PendingCount=>pending.Count;
        public void Begin(uint id,int kind,int count,float now,float duration,Action<T> deliver)
        {
            Expire(now);
            if(id==0 || casts.ContainsKey(id))return;
            casts[id]=new Cast {kind=kind,count=count,expires=now+duration+3};
            for(int i=0;i<pending.Count;)
            {
                var request=pending[i];
                if(request.cast!=id) {i++;continue;}
                pending.RemoveAt(i);Dispatch(request,deliver);
            }
        }
        public void Submit(uint id,int kind,int index,float now,T payload,Action<T> deliver)
        {
            Expire(now);
            if(id==0 || index<0 || index>=3)return;
            var request=new Request {cast=id,kind=kind,index=index,expires=now+3,payload=payload};
            if(casts.ContainsKey(id)) {Dispatch(request,deliver);return;}
            if(pending.Count>=12 || pending.Exists(p=>p.cast==id && p.kind==kind && p.index==index))return;
            pending.Add(request);
        }
        private void Dispatch(Request request,Action<T> deliver)
        {
            var cast=casts[request.cast];
            if(request.kind!=cast.kind || request.index>=cast.count || (cast.mask&(1<<request.index))!=0)return;
            cast.mask|=1<<request.index;deliver(request.payload);
        }
        public void Cancel(uint id,float now) { if(id==0)return;Expire(now);if(casts.TryGetValue(id,out var c))c.mask=7;else casts[id]=new Cast{mask=7,count=0,expires=now+3};pending.RemoveAll(p=>p.cast==id); }
        public void Expire(float now)
        {
            pending.RemoveAll(p=>p.expires<now);
            var expired=new List<uint>();
            foreach(var pair in casts)if(pair.Value.expires<now)expired.Add(pair.Key);
            foreach(var key in expired)casts.Remove(key);
        }
    }
}
