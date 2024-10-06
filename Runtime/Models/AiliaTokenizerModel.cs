/* ailia.tokenizer model class */
/* Copyright 2023 - 2024 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Threading;
using System.Runtime.InteropServices;

namespace ailiaTokenizer{
public class AiliaTokenizerModel : IDisposable
{
	// instance
	IntPtr net = IntPtr.Zero;
	bool logging = true;

	/****************************************************************
	 * モデル
	 */

	/**
	* \~japanese
	* @brief インスタンスを作成します。
	* @param type           タイプ（AiliaTokenizer.AILIA_TOKENIZER_TYPE_*)
	* @param flag           フラグの論理和（AiliaTokenizer.AILIA_TOKENIZER_FLAG_*)
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Create a instance.
	* @param type           Type (AiliaTokenizer..AILIA_TOKENIZER_TYPE_*)
	* @param flag           OR of flags (AiliaTokenizer..AILIA_TOKENIZER_FLAG_*)
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool Create(int type, int flag){
		if (net != IntPtr.Zero){
			Close();
		}

		int status = AiliaTokenizer.ailiaTokenizerCreate(ref net, type, flag);
		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerCreate failed " + status);
			}
			return false;
		}

		return true;
	}

	/**
	* \~japanese
	* @brief モデルファイルを開きます。
	* @param model_path          モデルファイルへのパス。(nullの場合は読み込まない)
	* @param dictionary_path     辞書ファイルへのパス。(nullの場合は読み込まない)
	* @param vocab_path          Vocabファイルへのパス。(nullの場合は読み込まない)
	* @param merge_path          Mergeファイルへのパス。(nullの場合は読み込まない)
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Open a model.
	* @param model_path          Path for model (don't load if null)
	* @param dictionary_path     Path for dictionary (don't load if null)
	* @param vocab_path          Path for vocab (don't load if null)
	* @param merge_path          Path for merge (don't load if null)
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool Open(string model_path = null, string dictionary_path = null, string vocab_path = null, string merge_path = null){
		if (net == IntPtr.Zero){
			return false;
		}

		int status = 0;
		
		if (model_path != null){
			status = AiliaTokenizer.ailiaTokenizerOpenModelFile(net, model_path);
			if (status != 0){
				if (logging){
					Debug.Log("ailiaTokenizerOpenModelFile failed " + status);
				}
				return false;
			}
		}
		if (dictionary_path != null){
			status = AiliaTokenizer.ailiaTokenizerOpenDictionaryFile(net, dictionary_path);
			if (status != 0){
				if (logging){
					Debug.Log("ailiaTokenizerOpenDictionaryFile failed " + status);
				}
				return false;
			}
		}
		if (vocab_path != null){
			status = AiliaTokenizer.ailiaTokenizerOpenVocabFile(net, vocab_path);
			if (status != 0){
				if (logging){
					Debug.Log("ailiaTokenizerOpenVocabFile failed " + status);
				}
				return false;
			}
		}
		if (merge_path != null){
			status = AiliaTokenizer.ailiaTokenizerOpenMergeFile(net, merge_path);
			if (status != 0){
				if (logging){
					Debug.Log("ailiaTokenizerOpenMergeFile failed " + status);
				}
				return false;
			}
		}

		return true;
	}

	/****************************************************************
	 * 開放する
	 */
	/**
	* \~japanese
	* @brief インスタンスを破棄します。
	* @details
	*   インスタンスを破棄し、初期化します。
	*   
	*  \~english
	* @brief   Destroys instance
	* @details
	*   Destroys and initializes the instance.
	*/
	public virtual void Close()
	{
		if (net != IntPtr.Zero){
			AiliaTokenizer.ailiaTokenizerDestroy(net);
			net = IntPtr.Zero;
		}
	}

	/**
	* \~japanese
	* @brief リソースを解放します。
	*   
	*  \~english
	* @brief   Release resources.
	*/
	public virtual void Dispose()
	{
		Dispose(true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing){
			// release managed resource
		}
		Close(); // release unmanaged resource
	}

	~AiliaTokenizerModel(){
		Dispose(false);
	}

	/****************************************************************
	 * エンコードとデコード
	 */

	private int[] EncodeCore(string utf8, bool special_tokens)
	{
		byte[] text = System.Text.Encoding.UTF8.GetBytes(utf8+"\u0000");
		GCHandle handle = GCHandle.Alloc(text, GCHandleType.Pinned);
		IntPtr input = handle.AddrOfPinnedObject();
		int status;
		if (special_tokens){
			status = AiliaTokenizer.ailiaTokenizerEncodeWithSpecialTokens(net, input);
		}else{
			status = AiliaTokenizer.ailiaTokenizerEncode(net, input);
		}
		handle.Free();
		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerEncode or ailiaTokenizerEncodeWithSpecialTokens failed " + status);
			}
			return new int[0];
		}
		uint count = 0;
		status = AiliaTokenizer.ailiaTokenizerGetTokenCount(net, ref count);
		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerGetTokenCount failed " + status);
			}
			return new int[0];
		}
		int[] tokens = new int [count];
		handle = GCHandle.Alloc(tokens, GCHandleType.Pinned);
		IntPtr output = handle.AddrOfPinnedObject();
		status = AiliaTokenizer.ailiaTokenizerGetTokens(net, output, count);
		handle.Free();
		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerGetTokens failed " + status);
			}
			return new int[0];
		}
		return tokens;
	}

	private string DecodeCore(int[] tokens, bool special_tokens)
	{
		uint count = (uint)tokens.Length;
		GCHandle handle = GCHandle.Alloc(tokens, GCHandleType.Pinned);
		IntPtr input = handle.AddrOfPinnedObject();
		int status;
		if (special_tokens){
			status = AiliaTokenizer.ailiaTokenizerDecodeWithSpecialTokens(net, input, count);
		} else {
			status = AiliaTokenizer.ailiaTokenizerDecode(net, input, count);
		}
		handle.Free();
		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerDecode or ailiaTokenizerDecodeWithSpecialTokens failed " + status);
			}
			return "";
		}
		uint len = 0;
		status = AiliaTokenizer.ailiaTokenizerGetTextLength(net, ref len);
		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerGetTextLength failed " + status);
			}
			return "";
		}
		byte[] text = new byte [len];
		handle = GCHandle.Alloc(text, GCHandleType.Pinned);
		IntPtr output = handle.AddrOfPinnedObject();
		status = AiliaTokenizer.ailiaTokenizerGetText(net, output, len);
		handle.Free();
		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerGetText failed " + status);
			}
			return "";
		}
		byte[] text_split = new byte [len - 1]; // NULLも時の削除
		for (int i = 0; i < len - 1; i++){
			text_split[i] = text[i];
		}
		return System.Text.Encoding.UTF8.GetString(text_split);
	}

	/**
	* \~japanese
	* @brief エンコードを実行します。
	* @param utf8    入力文字列
	* @return
	*   成功した場合はトークン列、失敗した場合は空配列を返す。
	*   
	* \~english
	* @brief   Perform encode
	* @param utf8    Input string
	* @return
	*   If this function is successful, it returns array of tokens  , or  empty array  otherwise.
	*/
	public int[] Encode(string utf8)
	{
		return EncodeCore(utf8, false);
	}

	/**
	* \~japanese
	* @brief スペシャルトークンを含んだエンコードを実行します。
	* @param utf8    入力文字列
	* @return
	*   成功した場合はトークン列、失敗した場合は空配列を返す。
	*   
	* \~english
	* @brief   Perform encode with special tokens
	* @param utf8    Input string
	* @return
	*   If this function is successful, it returns array of tokens  , or  empty array  otherwise.
	*/
	public int[] EncodeWithSpecialTokens(string utf8)
	{
		return EncodeCore(utf8, true);
	}

	/**
	* \~japanese
	* @brief デコードを実行します。
	* @pram tokens   入力トークン
	* @return
	*   成功した場合は文字列、失敗した場合は空文字列を返す。
	*   
	* \~english
	* @brief   Perform decode
	* @pram tokens   Input tokens
	* @return
	*   If this function is successful, it returns  string  , or  empty string  otherwise.
	*/
	public string Decode(int[] tokens)
	{
		return DecodeCore(tokens, false);
	}

	/**
	* \~japanese
	* @brief スペシャルトークンを含んだデコードを実行します。
	* @pram tokens   入力トークン
	* @return
	*   成功した場合は文字列、失敗した場合は空文字列を返す。
	*   
	* \~english
	* @brief   Perform decode with special tokens
	* @pram tokens   Input tokens
	* @return
	*   If this function is successful, it returns  string  , or  empty string  otherwise.
	*/
	public string DecodeWithSpecialTokens(int[] tokens)
	{
		return DecodeCore(tokens, true);
	}

	/**
	* \~japanese
	* @brief Vocabの数を取得します。
	* @return
	*   成功した場合は 0以上の数値 、そうでなければ-1を返す。
	*
	* \~english
	* @brief Gets the size of vocab.
	* @return
	*   If this function is successful, it returns the size of vocab , or -1 otherwise.
	*/
	public int GetVocabSize()
	{
		uint len = 0;
		int status = AiliaTokenizer.ailiaTokenizerGetVocabSize(net, ref len);
		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerGetVocabSize failed " + status);
			}
			return -1;
		}
		return (int)len;
	}

	/**
	* \~japanese
	* @brief Vocabの取得を行います。
	* @param token トークン
	* @return
	*   成功した場合は string 、そうでなければnullを返す。
	*
	* \~english
	* @brief Acquiring vocab
	* @param token Token
	* @return
	*   If this function is successful, it returns string , or null otherwise.
	*/
	public string GetVocab(int token)
	{
		IntPtr ptr = IntPtr.Zero;
		int status = AiliaTokenizer.ailiaTokenizerGetVocab(net, token, ref ptr);
		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerGetVocab failed " + status);
			}
			return null;
		}
		return Marshal.PtrToStringAnsi(ptr);
	}

	/**
	* \~japanese
	* @brief SpecialTokenの追加を行います。
	* @param tokens トークン
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
    * @details
    *   AILIA_TOKENIZER_TYPE_ROBERTAの場合のみ有効です。
	*
	* \~english
	* @brief Adding SpecialToken.
	* @param tokens Token
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
    * @details
    *   This is valid only for AILIA_TOKENIZER_TYPE_ROBERTA and AILIA_TOKENIZER_TYPE_ROBERTA.
	*/
	public bool AddSpecialTokens(string [] tokens)
	{
		IntPtr[] utf8Strings = new IntPtr[tokens.Length];
		for (int i = 0; i < tokens.Length; i++) {
			byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(tokens[i] + '\0');
			utf8Strings[i] = Marshal.AllocHGlobal(utf8Bytes.Length);
			Marshal.Copy(utf8Bytes, 0, utf8Strings[i], utf8Bytes.Length);
		}

		IntPtr tokensPtr = Marshal.AllocHGlobal(IntPtr.Size * tokens.Length);
		Marshal.Copy(utf8Strings, 0, tokensPtr, utf8Strings.Length);

		int status = AiliaTokenizer.ailiaTokenizerAddSpecialTokens(net, tokensPtr, (uint)tokens.Length);

		Marshal.FreeHGlobal(tokensPtr);
		foreach (IntPtr ptr in utf8Strings) {
			Marshal.FreeHGlobal(ptr);
		}

		if (status != 0){
			if (logging){
				Debug.Log("ailiaTokenizerAddSpecialTokens failed " + status);
			}
			return false;
		}
		return true;
	}
}
} // namespace ailiaTokenizer
