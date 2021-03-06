﻿(*-------------------------------------------------------------------------
Copyright (c) Paulmichael Blasucci.                                        
                                                                           
This source code is subject to terms and conditions of the Apache License, 
Version 2.0. A copy of the license can be found in the License.html file   
at the root of this distribution.                                          
                                                                           
By using this source code in any fashion, you are agreeing to be bound     
by the terms of the Apache License, Version 2.0.                           
                                                                           
You must not remove this notice, or any other, from this software.         
-------------------------------------------------------------------------*)
namespace fszmq

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

/// Encapsulates data generated by various ZMQ monitoring events
type ZMQEvent = 
  { Source  : nativeint
    Event   : int
    Address : string 
    Details : int } 
  with
    static member internal Build(source,event,data:C.zmq_event_data_t) =
      { Source  = source
        Event   = event
        Address = data.address
        Details = data.details }


/// Contains methods for working with Context instances
[<Extension;
  CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Context =

(* socket types *)

  /// Creates a Socket, of the given type, within the given context 
  [<Extension;CompiledName("Socket")>]
  let newSocket (context:Context) socketType = 
    new Socket(context.Handle,socketType)

  /// <summary>
  /// Creates a peer connected to exactly one other peer.
  /// <para>This socket type is used primarily for inter-thread 
  /// communication across the "inproc" transport.</para>
  /// </summary>
  [<Extension;CompiledName("Pair")>]
  let pair (context:Context) = ZMQ.PAIR |> newSocket context

  /// <summary>
  /// Creates a client for sending requests to and receiving replies from 
  /// a service.
  /// <para>This socket type allows only an alternating sequence of 
  /// `Socket.send(request)` followed by `Socket.recv(reply)` calls.</para>
  /// </summary>
  [<Extension;CompiledName("Request")>]
  let req (context:Context) = ZMQ.REQ |> newSocket context
 
  /// <summary>
  /// Creates a service to receive requests from and send replies to a 
  /// client.
  /// <para>This socket type allows only an alternating sequence of 
  /// `Socket.recv(reply)` followed by `Socket.send(request)` calls.</para>
  /// </summary>
  [<Extension;CompiledName("Response")>]
  let rep (context:Context) = ZMQ.REP |> newSocket context

  /// <summary>
  /// Creates an advanced socket type used for extending the request/reply 
  /// pattern.
  /// <para>When a ZMQ.DEALER socket is connected to a ZMQ.REP socket,
  /// each message sent must consist of an empty message part, the 
  /// delimiter, followed by one or more body parts.</para>
  /// </summary>
  [<Extension;CompiledName("Dealer")>]
  let deal (context:Context) = ZMQ.DEALER |> newSocket context
  
  /// <summary>
  /// Creates an advanced socket type used for extending the request/reply 
  /// pattern. 
  /// <para>When receiving messages a ZMQ.ROUTER socket prepends a 
  /// message part containing the identity of the originating peer.</para>
  /// <para>When sending messages a ZMQ.ROUTER socket removes the first 
  /// part of the message and uses it to determine the identity of 
  /// the recipient.</para>
  /// </summary>
  [<Extension;CompiledName("Router")>]
  let route (context:Context) = ZMQ.ROUTER |> newSocket context
  
  /// Creates a pipeline node to receive messages from upstream (PUSH) nodes.
  [<Extension;CompiledName("Pull")>]
  let pull (context:Context) = ZMQ.PULL |> newSocket context
  
  /// Creates a pipeline node to send messages to downstream (PULL) nodes.
  [<Extension;CompiledName("Push")>]
  let push (context:Context) = ZMQ.PUSH |> newSocket context
  
  /// <summary>
  /// Creates a publisher used to distribute messages to subscribers.
  /// <para>NOTE: topical filtering will be done by the subscriber</para>
  /// </summary>
  [<Extension;CompiledName("Publish")>]
  let pub (context:Context) = ZMQ.PUB |> newSocket context
  
  /// <summary>
  /// Creates a subscriber to receive to data distributed by a publisher.
  /// <para>Initially a ZMQ.SUB socket is not subscribed to any messages 
  /// (i.e. one, or more, subscriptions must be manually applied before 
  /// any messages will be received).</para>
  /// </summary>
  [<Extension;CompiledName("Subscribe")>]
  let sub (context:Context) = ZMQ.SUB |> newSocket context

  /// Behaves the same as a publisher, except topical filtering is done
  /// by the publisher (before sending a message)
  [<Extension;CompiledName("PublishEx")>]
  let xpub (context:Context) = ZMQ.XPUB |> newSocket context
  
  /// <summary>
  /// Behaves the same as a subscriber, except topical filtering is done
  /// by the publisher (before sending a message)
  /// <para>NOTE: subscriptions are made by sending a subscription message,
  /// in which the first byte is 1 or 0 (subscribe or unsubscribe) 
  /// and the remainder of the message is the topic</para>
  /// </summary>
  [<Extension;CompiledName("SubscribeEx")>]
  let xsub (context:Context) = ZMQ.XSUB |> newSocket context

(* context options *)
  
  /// Gets the value of the given option for the given Context
  [<Extension;CompiledName("GetOption")>]
  let get (context:Context) contextOption =
    let okay = C.zmq_ctx_get(context.Handle,contextOption)
    if  okay = -1 then ZMQ.error()

   /// Sets the given option value for the given Context
  [<Extension;CompiledName("SetOption")>]
  let set (context:Context) (contextOption,value) =
    let okay = C.zmq_ctx_set(context.Handle,contextOption,value)
    if  okay <> 0 then ZMQ.error()

  /// Sets the given block of option values for the given Context
  [<Extension;CompiledName("Configure")>]
  let config (context:Context) (options:seq<int * int>) =
    let set' = set context
    options |> Seq.iter (fun input -> set' input)
    
(* monitoring *)

  /// <summary>
  /// Registers a callback function which will receive 
  /// status events raised by the given Context (and its Sockets)
  /// <para>NOTE: this functionality is intended for monitoring only --
  /// excessive time spent in the callback will block the sending and
  /// receiving of messages for the Socket passed via the event</para>
  /// </summary>
  [<Extension;CompiledName("SetMonitor")>]
  let monitor (context:Context) callback =
    let bind = (fun s e d -> ZMQEvent.Build(s,e,d) |> callback)
    let okay = C.zmq_ctx_set_monitor(context.Handle,C.zmq_monitor_fn(bind))
    if  okay <> 0 then ZMQ.error()
