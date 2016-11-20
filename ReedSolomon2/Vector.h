#pragma once
#include <cstdint>

namespace ReedSolomon {

	class Vector {

	public:

		Vector(int length);
		Vector(const Vector& v);

		~Vector();

		void operator=(const Vector& v);

		Vector& operator+=(const Vector& v);
		Vector& operator*=(uint16_t a);
		Vector operator*(uint16_t a) const;

		inline uint16_t operator[](int i) const {
			return elements[i];
		}

		inline uint16_t& operator[](int i) {
			return elements[i];
		}

		inline int GetLength() const {
			return length;
		}

	protected:

		uint16_t* elements;

	private:

		int length;
	};

	Vector operator*(uint16_t a, const Vector& v);

	uint16_t InnerProduct(const Vector& a, const Vector& b);
}