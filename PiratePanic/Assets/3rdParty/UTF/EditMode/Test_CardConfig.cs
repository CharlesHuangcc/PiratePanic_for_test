using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PiratePanic;
using System.Drawing.Printing;

[TestFixture]
public class Test_CardConfig
{
    List<CardInfo> _cards=null;

    [SetUp]
    public void CaseSetUp()
    {
        _cards=UTFConfigTestEditor.GetAllConfig<CardInfo>("Assets/PiratePanic/ScriptableObjects");
    }

    [TearDown]
    public void CaseTearDown()
    {
        _cards=null;
    }

    [Test]
    public void Test_CardHealth()
    {
        foreach(CardInfo card in _cards)
        {
            Debug.Log(card.Health);
            Assert.IsTrue(card.Health>0,$"{card.name}的Health需要大于0");
        }
    }

    [Test]
    public void Test_CardDamage()
    {
        foreach(CardInfo card in _cards)
        {
            Assert.IsTrue(card.Damage>=0,$"{card.name}的Damage需要大于等于0");
        }
    }

    [Test]
    public void Test_CardAttackeType()
    {
        foreach(CardInfo card in _cards)
        {
            Assert.IsNotEmpty(card.AttackeType.ToString(),$"{card.name}的AttackeType不能为空");
        }
    }

    [Test]
    public void Test_CardReloadTime()
    {
        foreach(CardInfo card in _cards)
        {
            Assert.IsTrue(card.ReloadTime>0,$"{card.name}的ReloadTime需要大于0");
        }
    }

    [Test]
    public void Test_CardMoveSpeed()
       {
        foreach(CardInfo card in _cards)
        {
            Assert.IsTrue(card.MoveSpeed>0,$"{card.name}的MoveSpeed需要大于0");
        }
    }

    [Test]
    public void Test_CardRotationSpeed()
       {
        foreach(CardInfo card in _cards)
        {
            Assert.IsTrue(card.RotationSpeed>0,$"{card.name}的RotationSpeed需要大于0");
        }
    }

}
